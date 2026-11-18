using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Csv;
using modelDto = TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public class TransactionService(TaxFilerContext taxFilerContext, IDocumentMatchingService documentMatchingService) : ITransactionService
{
    public IEnumerable<TransactionDto> ParseTransactions(TextReader reader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";"
        };
        using var csv = new CsvReader(reader, config);
        var transactions = csv.GetRecords<TransactionDto>();

        var retVaue = transactions.Select(t => new TransactionDto
                                  {
                                      BookingDate = DateTime.SpecifyKind(t.BookingDate, DateTimeKind.Utc),
                                      SenderReceiver = t.SenderReceiver,
                                      CounterPartyBIC = t.CounterPartyBIC,
                                      CounterPartyIBAN = t.CounterPartyIBAN,
                                      Comment = t.Comment,
                                      Amount = t.Amount / 100
                                  }
                              ).ToArray();

        return retVaue;
    }

    public async Task<MemoryStream> CreateCsvFileAsync(DateOnly yearMonth)
    {
        var startOfMonth = new DateTime(yearMonth.Year, yearMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = new DateTime(yearMonth.Year, yearMonth.Month,
            DateTime.DaysInMonth(yearMonth.Year, yearMonth.Month), 0, 0, 0, DateTimeKind.Utc);

        var transactions = await taxFilerContext
            .Transactions.Include(t => t.TransactionDocuments).ThenInclude(td => td.Document)
            .Where(t => t.TransactionDateTime >= startOfMonth && t.TransactionDateTime <= endOfMonth
                                                              && (t.IsSalesTaxRelevant == true ||
                                                                  t.IsIncomeTaxRelevant == true))
            .OrderBy(t => t.TransactionDateTime)
            .ToListAsync();

        var transactionReportDtos = transactions.Select
        (t => new TranactionReportDto
            {
                NetAmount = t.NetAmount ?? 0m,
                GrossAmount = t.GrossAmount,
                TaxAmount = t.TaxAmount ?? 0m,
                TaxRate = t.TaxRate ?? 0m,
                TransactionReference = t.TransactionReference,
                TransactionDateTime = t.TransactionDateTime,
                TransactionNote = t.TransactionNote,
                IsOutgoing = t.IsOutgoing,
                IsIncomeTaxRelevant = t.IsIncomeTaxRelevant ?? false,
                IsSalesTaxRelevant = t.IsSalesTaxRelevant ?? false,
                DocumentName = t.IsOutgoing
                    ? $"Rechnungseingang/{PrimaryDocument(t)?.Name}"
                    : $"Rechnungsausgang/{PrimaryDocument(t)?.Name}",
                SenderReceiver = t.SenderReceiver
            }
        ).ToList();

        var memoryStream = new MemoryStream();

        await using var writer = new StreamWriter(memoryStream, leaveOpen: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            Encoding = Encoding.UTF8
        };

        await using var csv = new CsvWriter(writer, config);

        await csv.WriteRecordsAsync(transactionReportDtos);
        await writer.FlushAsync();

        return memoryStream;
    }


    public async Task AddTransactionsAsync(IEnumerable<TransactionDto> transactions)
    {
        try
        {
            foreach (var transaction in transactions)
            {
                if (taxFilerContext.Transactions.Any(t => t.TransactionDateTime == transaction.BookingDate
                                                          && t.Counterparty == transaction.CounterPartyIBAN
                                                          && t.TransactionNote == transaction.Comment
                                                          && t.GrossAmount == Math.Abs(transaction.Amount)))
                {
                    continue;
                }

                var transactionDb = transaction.ToTransaction();
                transactionDb.IsOutgoing = transactionDb.GrossAmount < 0;
                transactionDb.GrossAmount = Math.Abs(transactionDb.GrossAmount);

                taxFilerContext.Transactions.Add(transactionDb);
            }

            await taxFilerContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task DeleteTransactionAsync(int id)
    {
        var transaction = await taxFilerContext.Transactions.FindAsync(id);
        if (transaction != null)
        {
            // Remove the many-to-many links first so the transaction can be deleted
            // cleanly (the FK cascade also handles this at the database level).
            var links = await taxFilerContext.TransactionDocuments
                .Where(td => td.TransactionId == id)
                .ToListAsync();
            taxFilerContext.TransactionDocuments.RemoveRange(links);

            taxFilerContext.Transactions.Remove(transaction);
            await taxFilerContext.SaveChangesAsync();
        }
    }


    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync(DateOnly yearMonth,
        int? accountId = null)
    {
        var query = taxFilerContext
            .Transactions
            .Include(t => t.TransactionDocuments).ThenInclude(td => td.Document)
            .Include(t => t.Account)
            .Where(t => t.TransactionDateTime.Year == yearMonth.Year && t.TransactionDateTime.Month == yearMonth.Month);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        var transactions = await query.ToListAsync();
        return transactions.Select(t => t.TransactionDto()).ToList();
    }

    public async Task<modelDto.TransactionDto> GetTransactionAsync(int transactionId)
    {
        var transaction = await taxFilerContext.Transactions
            .Include(t => t.TransactionDocuments).ThenInclude(td => td.Document)
            .Include(a => a.Account)
            .SingleAsync(t => t.Id == transactionId);
        return transaction.TransactionDto();
    }

    public async Task UpdateTransactionAsync(modelDto.UpdateTransactionDto updateTransactionDto)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == updateTransactionDto.Id);

        TransactionMapper.UpdateTransaction(transaction, updateTransactionDto);
        await taxFilerContext.SaveChangesAsync();
    }

    public async Task DeleteTransactionsAsync(DateTime yearMonth)
        => await taxFilerContext
            .Transactions
            .Where(t => t.TaxYear == yearMonth.Year && t.TaxMonth == yearMonth.Month)
            .ExecuteDeleteAsync();

    /// <inheritdoc />
    public async Task<Result<modelDto.TransactionDocumentDto>> AddDocumentToTransactionAsync(
        int transactionId,
        modelDto.AddDocumentToTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var transaction = await taxFilerContext.Transactions
            .Include(t => t.TransactionDocuments)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null)
        {
            return Result.Fail($"Transaction {transactionId} not found");
        }

        var document = await taxFilerContext.Documents
            .FirstOrDefaultAsync(d => d.Id == dto.DocumentId, cancellationToken);

        if (document == null)
        {
            return Result.Fail($"Document {dto.DocumentId} not found");
        }

        if (transaction.TransactionDocuments.Any(td => td.DocumentId == dto.DocumentId))
        {
            return Result.Fail($"Document {dto.DocumentId} is already linked to transaction {transactionId}");
        }

        var existing = transaction.TransactionDocuments
            .OrderBy(td => td.Sequence)
            .ToList();

        var maxSequence = existing.Count == 0 ? 0 : existing.Max(td => td.Sequence);

        // Append by default. An explicit sequence is honored only when it sits beyond
        // the current end of the collection, which avoids renumbering collisions with
        // the per-transaction unique (TransactionId, Sequence) constraint.
        var sequence = dto.Sequence is > 0 && dto.Sequence > maxSequence
            ? dto.Sequence.Value
            : maxSequence + 1;

        var association = new TransactionDocument
        {
            TransactionId = transactionId,
            DocumentId = document.Id,
            Sequence = sequence,
            AssignedAt = DateTime.UtcNow,
            Notes = dto.Notes
        };

        taxFilerContext.TransactionDocuments.Add(association);

        // When the document becomes the primary (first) document, mirror its tax
        // data onto the transaction, matching the legacy single-document behavior.
        if (sequence == 1)
        {
            ApplyDocumentTaxData(transaction, document.SubTotal, document.TaxAmount, document.TaxRate, document.Skonto);
        }

        await taxFilerContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(association.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result> RemoveDocumentFromTransactionAsync(
        int transactionId,
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var link = await taxFilerContext.TransactionDocuments
            .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId, cancellationToken);

        if (link == null)
        {
            return Result.Fail($"Document {documentId} is not linked to transaction {transactionId}");
        }

        taxFilerContext.TransactionDocuments.Remove(link);
        await taxFilerContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<modelDto.TransactionDocumentDto>> GetTransactionDocumentsAsync(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        var links = await taxFilerContext.TransactionDocuments
            .Include(td => td.Document)
            .Where(td => td.TransactionId == transactionId)
            .OrderBy(td => td.Sequence)
            .ToListAsync(cancellationToken);

        return links.Select(td => td.ToDto()).ToList();
    }

    public async Task<modelDto.AutoAssignResult> AutoAssignDocumentsAsync(DateOnly yearMonth, CancellationToken cancellationToken = default)
    {
        // 1. Query unmatched transactions for the month
        var unmatchedTransactions = await taxFilerContext.Transactions
            .Include(t => t.TransactionDocuments)
            .Where(t => t.TransactionDateTime.Year == yearMonth.Year 
                     && t.TransactionDateTime.Month == yearMonth.Month
                     && !t.TransactionDocuments.Any())
            .OrderBy(t => t.TransactionDateTime)
            .ToListAsync(cancellationToken);
        
        // 2. Use batch matching to get candidates for all transactions
        var matchesByTransaction = await documentMatchingService
            .BatchDocumentMatchesAsync(unmatchedTransactions, cancellationToken);
        
        // 3. Process each transaction and assign best match if score >= 0.5
        int assignedCount = 0;
        int skippedCount = 0;
        var errors = new List<string>();
        
        foreach (var transaction in unmatchedTransactions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            try
            {
                if (matchesByTransaction.TryGetValue(transaction.Id, out var matches))
                {
                    var bestMatch = matches.FirstOrDefault();
                    
                    if (bestMatch != null && bestMatch.MatchScore >= 0.5)
                    {
                        var document = bestMatch.Document;
                        var association = new TransactionDocument
                        {
                            TransactionId = transaction.Id,
                            DocumentId = document.Id,
                            Sequence = 1,
                            AssignedAt = DateTime.UtcNow
                        };
                        taxFilerContext.TransactionDocuments.Add(association);

                        // Copy tax data from document to transaction (same logic as AddDocumentToTransactionAsync)
                        ApplyDocumentTaxData(transaction, document.SubTotal, document.TaxAmount, document.TaxRate, document.Skonto);
                        
                        assignedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Transaction {transaction.Id}: {ex.Message}");
                skippedCount++;
                // Continue processing remaining transactions
            }
        }
        
        // 4. Save all changes in a single transaction
        if (assignedCount > 0)
        {
            await taxFilerContext.SaveChangesAsync(cancellationToken);
        }
        
        // 5. Return summary
        return new modelDto.AutoAssignResult
        {
            TotalProcessed = unmatchedTransactions.Count,
            AssignedCount = assignedCount,
            SkippedCount = skippedCount,
            Errors = errors
        };
    }

    /// <summary>
    /// Returns the first (primary) document linked to the transaction, or null.
    /// </summary>
    private static Document? PrimaryDocument(Transaction transaction)
    {
        return transaction.TransactionDocuments?
            .OrderBy(td => td.Sequence)
            .Select(td => td.Document)
            .FirstOrDefault();
    }

    /// <summary>
    /// Mirrors a document's tax-relevant fields onto the transaction, applying the
    /// Skonto discount when present. Mirrors the legacy single-document behavior.
    /// </summary>
    private static void ApplyDocumentTaxData(
        Transaction transaction,
        decimal? subTotal,
        decimal? taxAmount,
        decimal? taxRate,
        decimal? skonto)
    {
        if (skonto is > 0)
        {
            var netAmountSkonto = subTotal.GetValueOrDefault() *
                (100 - skonto.GetValueOrDefault()) / 100;

            transaction.NetAmount = Math.Round(netAmountSkonto, 2);
            transaction.TaxRate = taxRate.GetValueOrDefault();
            transaction.TaxAmount = transaction.GrossAmount - transaction.NetAmount;
        }
        else
        {
            transaction.NetAmount = subTotal.GetValueOrDefault();
            transaction.TaxAmount = taxAmount.GetValueOrDefault();
            transaction.TaxRate = taxRate.GetValueOrDefault();
        }
    }
}
