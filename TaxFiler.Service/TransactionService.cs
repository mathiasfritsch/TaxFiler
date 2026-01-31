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
        
        // 2. Process each transaction using the new multiple document matching service
        int assignedCount = 0;
        int skippedCount = 0;
        var errors = new List<string>();
        
        foreach (var transaction in unmatchedTransactions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            try
            {
                // Use the new AutoAssignMultipleDocumentsAsync method
                var assignmentResult = await documentMatchingService
                    .AutoAssignMultipleDocumentsAsync(transaction.Id, cancellationToken);
                
                if (assignmentResult.IsSuccess && assignmentResult.Value.DocumentsAttached > 0)
                {
                    assignedCount++;
                    
                    // Log successful assignment
                    if (assignmentResult.Value.HasWarnings)
                    {
                        errors.AddRange(assignmentResult.Value.Warnings.Select(w => 
                            $"Transaction {transaction.Id}: {w}"));
                    }
                }
                else
                {
                    skippedCount++;
                    
                    // Log why assignment failed
                    if (assignmentResult.IsFailed)
                    {
                        errors.Add($"Transaction {transaction.Id}: {string.Join(", ", assignmentResult.Errors.Select(e => e.Message))}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Transaction {transaction.Id}: {ex.Message}");
                skippedCount++;
                // Continue processing remaining transactions
            }
        }
        
        // 3. Return summary
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