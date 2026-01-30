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
            .Transactions.Include(t => t.Document)
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
                    ? $"Rechnungseingang/{t.Document?.Name}"
                    : $"Rechnungsausgang/{t.Document?.Name}",
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
            .Include(t => t.Document)
            .Include(t => t.Account)
            .Include(t => t.TransactionDocuments)
                .ThenInclude(td => td.Document)
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
            .Include(a => a.Account)
            .Include(t => t.TransactionDocuments)
                .ThenInclude(td => td.Document)
            .SingleAsync(t => t.Id == transactionId);
        return transaction.TransactionDto();
    }

    public async Task UpdateTransactionAsync(modelDto.UpdateTransactionDto updateTransactionDto)
    {
        var transaction = await taxFilerContext.Transactions
            .Include(t => t.TransactionDocuments)
            .SingleAsync(t => t.Id == updateTransactionDto.Id);

        // Handle multiple documents if provided
        if (updateTransactionDto.DocumentIds != null && updateTransactionDto.DocumentIds.Any())
        {
            // Remove existing document associations
            taxFilerContext.TransactionDocuments.RemoveRange(transaction.TransactionDocuments);
            
            // Fetch all documents for the transaction
            var documents = await taxFilerContext.Documents
                .Where(d => updateTransactionDto.DocumentIds.Contains(d.Id))
                .ToListAsync();
            
            // Validate that at least one document was found
            if (!documents.Any())
            {
                throw new InvalidOperationException("No valid documents found for the provided document IDs");
            }
            
            // Create new associations
            foreach (var doc in documents)
            {
                transaction.TransactionDocuments.Add(new DB.Model.TransactionDocument
                {
                    TransactionId = transaction.Id,
                    DocumentId = doc.Id
                });
            }
            
            // Calculate totals from all documents
            // For Skonto: calculate per document, then sum the net amounts
            decimal totalNetAmount = 0;
            decimal totalTaxAmount = 0;
            
            foreach (var doc in documents)
            {
                if (doc.Skonto is > 0)
                {
                    var netAmountSkonto = doc.SubTotal.GetValueOrDefault() *
                        (100 - doc.Skonto.GetValueOrDefault()) / 100;
                    totalNetAmount += netAmountSkonto;
                }
                else
                {
                    totalNetAmount += doc.SubTotal.GetValueOrDefault();
                }
                totalTaxAmount += doc.TaxAmount.GetValueOrDefault();
            }
            
            // Use the tax rate from the first document
            // Note: This assumes all documents have the same tax rate
            // Consider validating this or using weighted average in the future
            var firstDocument = documents.FirstOrDefault();
            if (firstDocument != null)
            {
                updateTransactionDto.NetAmount = Math.Round(totalNetAmount, 2);
                updateTransactionDto.TaxAmount = totalTaxAmount;
                updateTransactionDto.TaxRate = firstDocument.TaxRate.GetValueOrDefault();
            }
            
            // Clear the single DocumentId for backward compatibility
            updateTransactionDto.DocumentId = null;
        }
        // Fall back to single document if no DocumentIds provided but DocumentId is set
        else if (transaction.DocumentId != updateTransactionDto.DocumentId && updateTransactionDto.DocumentId > 0)
        {
            var document = await taxFilerContext.Documents.SingleAsync(d => d.Id == updateTransactionDto.DocumentId);

            if (document.Skonto is > 0)
            {
                var netAmountSkonto = document.SubTotal.GetValueOrDefault() *
                    (100 - document.Skonto.GetValueOrDefault()) / 100;

                updateTransactionDto.NetAmount = Math.Round(netAmountSkonto, 2);
                updateTransactionDto.TaxRate = document.TaxRate.GetValueOrDefault();
                updateTransactionDto.TaxAmount = updateTransactionDto.GrossAmount - updateTransactionDto.NetAmount;
            }
            else
            {
                updateTransactionDto.NetAmount = document.SubTotal.GetValueOrDefault();
                updateTransactionDto.TaxAmount = document.TaxAmount.GetValueOrDefault();
                updateTransactionDto.TaxRate = document.TaxRate.GetValueOrDefault();
            }
        }

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
        // 1. Query unmatched transactions for the month
        var unmatchedTransactions = await taxFilerContext.Transactions
            .Where(t => t.TransactionDateTime.Year == yearMonth.Year 
                     && t.TransactionDateTime.Month == yearMonth.Month
                     && t.DocumentId == null)
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
                        transaction.DocumentId = document.Id;
                        
                        // Copy tax data from document to transaction (same logic as UpdateTransactionAsync)
                        var skontoValue = document.Skonto.GetValueOrDefault();
                        if (skontoValue > 0)
                        {
                            var netAmountSkonto = document.SubTotal.GetValueOrDefault() *
                                (100 - skontoValue) / 100m;
                            
                            transaction.NetAmount = Math.Round(netAmountSkonto, 2);
                            transaction.TaxRate = document.TaxRate.GetValueOrDefault();
                            transaction.TaxAmount = transaction.GrossAmount - transaction.NetAmount;
                        }
                        else
                        {
                            transaction.NetAmount = document.SubTotal.GetValueOrDefault();
                            transaction.TaxAmount = document.TaxAmount.GetValueOrDefault();
                            transaction.TaxRate = document.TaxRate.GetValueOrDefault();
                        }
                        
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
}