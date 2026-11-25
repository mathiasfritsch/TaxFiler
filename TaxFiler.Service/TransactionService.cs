using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
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
        var options = new CsvReaderOptions
        {
            Separator = ';'
        };
        using var csv = CsvReader.Create(reader, options);
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
        var transactionReportDtos = await BuildReportAsync(yearMonth);

        var memoryStream = new MemoryStream();

        await using var writer = new StreamWriter(memoryStream, leaveOpen: true);

        await using var csv = CsvWriter.Create(writer);

        await csv.WriteRecordsAsync(transactionReportDtos);
        await writer.FlushAsync();

        return memoryStream;
    }

    public async Task<MemoryStream> CreateExcelFileAsync(DateOnly yearMonth)
    {
        var reportDtos = await BuildReportAsync(yearMonth);

        // "IsOutgoing" means money left the account, i.e. the statement shows a
        // payment to a supplier. In the USt-Voranmeldung template those rows sit
        // under "Eingangsrechnungen" (the "Rechnungseingang" documents).
        var incoming = reportDtos.Where(t => t.IsOutgoing).ToList();
        var outgoing = reportDtos.Where(t => !t.IsOutgoing).ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Zahlungen");

        SetColumnWidths(worksheet);

        var row = 1;
        row = WriteSection(worksheet, row, "Eingangsrechnungen", incoming);
        row = WriteSection(worksheet, row, "Ausgangsrechnungen / Gutschriften", outgoing);

        var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    private async Task<List<TranactionReportDto>> BuildReportAsync(DateOnly yearMonth)
    {
        var startOfMonth = new DateTime(yearMonth.Year, yearMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = new DateTime(yearMonth.Year, yearMonth.Month,
            DateTime.DaysInMonth(yearMonth.Year, yearMonth.Month), 0, 0, 0, DateTimeKind.Utc);

        var transactions = await taxFilerContext
            .Transactions.Include(t => t.TransactionDocuments).ThenInclude(td => td.Document)
            .Include(t => t.Account)
            .Where(t => t.TransactionDateTime >= startOfMonth && t.TransactionDateTime <= endOfMonth
                                                              && (t.IsSalesTaxRelevant == true ||
                                                                  t.IsIncomeTaxRelevant == true))
            .OrderBy(t => t.TransactionDateTime)
            .ToListAsync();

        return transactions.Select
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
                SenderReceiver = t.SenderReceiver,
                AccountName = t.Account.Name
            }
        ).ToList();
    }

    private const string DateFormat = "mm-dd-yy";
    private const string CurrencyFormat = "#,##0.00\\ \"€\"";

    private static readonly string[] ExcelHeaders =
    [
        "Buchungsdatum",
        "Auftraggeber/Empfänger",
        "Beschreibung",
        "Zahlungsbetrag",
        "Begleitende Dokumente",
        "Mwst Satz",
        "UST",
        "Kategorie",
        "Bemerkung",
        "Konto"
    ];

    private static void SetColumnWidths(IXLWorksheet worksheet)
    {
        worksheet.Column(1).Width = 32.55;
        worksheet.Column(2).Width = 36;
        worksheet.Column(3).Width = 113.78;
        worksheet.Column(4).Width = 43.44;
        worksheet.Column(5).Width = 87.78;
        worksheet.Column(6).Width = 10;
        worksheet.Column(7).Width = 21.22;
        worksheet.Column(8).Width = 44.55;
        worksheet.Column(9).Width = 40;
        worksheet.Column(10).Width = 13.44;
    }

    private static int WriteSection(IXLWorksheet worksheet, int startRow, string title,
        IReadOnlyList<TranactionReportDto> items)
    {
        var row = startRow;

        // Section title.
        worksheet.Cell(row, 1).Value = title;
        row++;

        // Header row (bold).
        for (var column = 0; column < ExcelHeaders.Length; column++)
        {
            var headerCell = worksheet.Cell(row, column + 1);
            headerCell.Value = ExcelHeaders[column];
            headerCell.Style.Font.Bold = true;
        }
        row++;

        // Data rows.
        var firstDataRow = row;
        foreach (var item in items)
        {
            worksheet.Cell(row, 1).Value = item.TransactionDateTime.Date;
            worksheet.Cell(row, 1).Style.NumberFormat.Format = DateFormat;
            worksheet.Cell(row, 2).Value = item.SenderReceiver;
            worksheet.Cell(row, 3).Value = item.TransactionNote;
            worksheet.Cell(row, 4).Value = item.GrossAmount;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = CurrencyFormat;
            worksheet.Cell(row, 5).Value = item.DocumentName;
            worksheet.Cell(row, 6).Value = item.TaxRate;
            worksheet.Cell(row, 7).Value = item.TaxAmount;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = CurrencyFormat;
            // "Kategorie" (column 8) and "Bemerkung" (column 9) are filled in
            // manually afterwards, so they are left empty here.
            worksheet.Cell(row, 10).Value = item.AccountName;
            row++;
        }

        var lastDataRow = row - 1;

        // Sum row totals the UST column (G).
        worksheet.Cell(row, 6).Value = "Summe";
        var sumCell = worksheet.Cell(row, 7);
        if (items.Count > 0)
        {
            sumCell.FormulaA1 = $"SUM(G{firstDataRow}:G{lastDataRow})";
        }
        else
        {
            sumCell.Value = 0m;
        }

        sumCell.Style.NumberFormat.Format = CurrencyFormat;

        return row + 1;
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
