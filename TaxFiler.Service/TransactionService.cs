using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
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

        return transactions.Select(t => new TransactionDto
            {
                BookingDate = DateTime.SpecifyKind(t.BookingDate, DateTimeKind.Utc),
                SenderReceiver = t.SenderReceiver,
                CounterPartyBIC = t.CounterPartyBIC,
                CounterPartyIBAN = t.CounterPartyIBAN,
                Comment = t.Comment,
                Amount = t.Amount / 100
            }
        ).ToArray();
    }

    public async Task<MemoryStream> CreateCsvFileAsync(DateOnly yearMonth)
    {
        var startOfMonth = new DateTime(yearMonth.Year, yearMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = new DateTime(yearMonth.Year, yearMonth.Month,
            DateTime.DaysInMonth(yearMonth.Year, yearMonth.Month), 0, 0, 0, DateTimeKind.Utc);

        var transactions = await taxFilerContext
            .Transactions
            .Include(t => t.DocumentAttachments)
                .ThenInclude(da => da.Document)
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
                    ? $"Rechnungseingang/{GetDocumentNamesForTransaction(t)}"
                    : $"Rechnungsausgang/{GetDocumentNamesForTransaction(t)}",
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
            taxFilerContext.Transactions.Remove(transaction);
            await taxFilerContext.SaveChangesAsync();
        }
    }


    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync(DateOnly yearMonth,
        int? accountId = null)
    {
        var query = taxFilerContext
            .Transactions
            .Include(t => t.DocumentAttachments)
                .ThenInclude(da => da.Document)
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
        var transaction = await taxFilerContext.Transactions.Include(a => a.Account).SingleAsync(t => t.Id == transactionId);
        return transaction.TransactionDto();
    }

    public async Task UpdateTransactionAsync(modelDto.UpdateTransactionDto updateTransactionDto)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == updateTransactionDto.Id);

        // Note: Document attachments are now managed through DocumentAttachmentService
        // Tax data copying from documents should be handled when documents are attached
        
        TransactionMapper.UpdateTransaction(transaction, updateTransactionDto);
        await taxFilerContext.SaveChangesAsync();
    }

    public async Task DeleteTransactionsAsync(DateTime yearMonth)
        => await taxFilerContext
            .Transactions
            .Where(t => t.TaxYear == yearMonth.Year && t.TaxMonth == yearMonth.Month)
            .ExecuteDeleteAsync();

    public async Task<modelDto.AutoAssignResult> AutoAssignDocumentsAsync(DateOnly yearMonth, CancellationToken cancellationToken = default)
    {
        // 1. Query unmatched transactions for the month (transactions without any document attachments)
        var unmatchedTransactionIds = await taxFilerContext.DocumentAttachments
            .Select(da => da.TransactionId)
            .ToListAsync(cancellationToken);
            
        var unmatchedTransactions = await taxFilerContext.Transactions
            .Where(t => t.TransactionDateTime.Year == yearMonth.Year 
                     && t.TransactionDateTime.Month == yearMonth.Month
                     && !unmatchedTransactionIds.Contains(t.Id))
            .OrderBy(t => t.TransactionDateTime)
            .ToListAsync(cancellationToken);

        if (!unmatchedTransactions.Any())
        {
            return new modelDto.AutoAssignResult
            {
                TotalProcessed = 0,
                AssignedCount = 0,
                SkippedCount = 0,
                Errors = new List<string>()
            };
        }

        // 2. Get matches for all unmatched transactions using batch processing
        var matchesByTransaction = await documentMatchingService
            .BatchDocumentMatchesAsync(unmatchedTransactions, cancellationToken);

        // 3. Process matches and create document attachments
        int assignedCount = 0;
        int skippedCount = 0;
        var errors = new List<string>();
        var transactionsToUpdate = new List<DB.Model.Transaction>();

        foreach (var transaction in unmatchedTransactions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Check if we have matches for this transaction
                if (!matchesByTransaction.TryGetValue(transaction.Id, out var matches) || !matches.Any())
                {
                    skippedCount++;
                    continue;
                }

                // Get the best match (highest score)
                var bestMatch = matches.OrderByDescending(m => m.MatchScore).First();

                // Only assign if the match score is above threshold (0.5)
                if (bestMatch.MatchScore < 0.5)
                {
                    skippedCount++;
                    continue;
                }

                // Create document attachment
                var attachment = new DB.Model.DocumentAttachment
                {
                    TransactionId = transaction.Id,
                    DocumentId = bestMatch.Document.Id,
                    AttachedAt = DateTime.UtcNow,
                    IsAutomatic = true,
                    AttachedBy = "AutoAssign"
                };

                taxFilerContext.DocumentAttachments.Add(attachment);

                // Copy tax data from document to transaction if available
                if (bestMatch.Document.SubTotal.HasValue)
                {
                    // Handle Skonto calculation if present
                    if (bestMatch.Document.Skonto.HasValue && bestMatch.Document.Skonto > 0)
                    {
                        // Calculate net amount with Skonto discount
                        var skontoMultiplier = (100 - bestMatch.Document.Skonto.Value) / 100;
                        transaction.NetAmount = bestMatch.Document.SubTotal.Value * skontoMultiplier;
                        
                        // Calculate tax amount from gross - net
                        transaction.TaxAmount = transaction.GrossAmount - transaction.NetAmount;
                    }
                    else
                    {
                        // No Skonto - copy values directly
                        transaction.NetAmount = bestMatch.Document.SubTotal;
                        transaction.TaxAmount = bestMatch.Document.TaxAmount;
                    }
                    
                    transaction.TaxRate = bestMatch.Document.TaxRate;
                    transactionsToUpdate.Add(transaction);
                }

                assignedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Transaction {transaction.Id}: {ex.Message}");
                skippedCount++;
            }
        }

        // 4. Save all changes
        if (assignedCount > 0)
        {
            await taxFilerContext.SaveChangesAsync(cancellationToken);
        }

        return new modelDto.AutoAssignResult
        {
            TotalProcessed = unmatchedTransactions.Count,
            AssignedCount = assignedCount,
            SkippedCount = skippedCount,
            Errors = errors
        };
    }

    /// <summary>
    /// Gets a comma-separated list of document names for a transaction.
    /// </summary>
    private static string GetDocumentNamesForTransaction(DB.Model.Transaction transaction)
    {
        if (transaction.DocumentAttachments == null || !transaction.DocumentAttachments.Any())
            return "No Documents";

        var documentNames = transaction.DocumentAttachments
            .Select(da => da.Document?.Name ?? "Unknown")
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        return documentNames.Any() ? string.Join(", ", documentNames) : "No Documents";
    }
}