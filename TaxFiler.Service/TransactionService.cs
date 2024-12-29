using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Csv;
using modelDto = TaxFiler.Model.Dto;
namespace TaxFiler.Service;

public class TransactionService(TaxFilerContext taxFilerContext):ITransactionService
{
    public IEnumerable<TransactionDto> ParseTransactions(TextReader reader)
    {
        //using var reader = new StreamReader("C:\\projects\\TaxFiler\\TaxFiler\\Finom_statement_25122024.csv");
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var transactions = csv.GetRecords<TransactionDto>();
        return transactions.ToArray();
    }

    public async Task AddTransactionsAsync(IEnumerable<TransactionDto> transactions)
    {
        foreach (var transaction in transactions)
        {
            taxFilerContext.Transactions.Add(
                new Transaction
                {
                    GrossAmount = transaction.Amount,
                    Counterparty = transaction.CounterPartyIBAN,
                    TransactionNote = transaction.Comment,
                    TransactionReference = transaction.TransactionID,
                    TransactionDateTime = new DateTime(transaction.BookingDate,transaction.TimeCompleted)
                }
            );
        }
        await taxFilerContext.SaveChangesAsync();
    }
    
    public async Task TruncateTransactionsAsync()
    {
        await taxFilerContext.Transactions.ExecuteDeleteAsync( );
    }

    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync()
    {
        var transactions = await taxFilerContext.Transactions.ToListAsync();
        return transactions.Select(t => new modelDto.TransactionDto
        {
            Id = t.Id,
            NetAmount = t.NetAmount,
            GrossAmount = t.GrossAmount,
            TaxAmount = t.TaxAmount,
            TaxRate = t.TaxRate,
            Counterparty = t.Counterparty,
            TransactionNote = t.TransactionNote,
            TransactionReference = t.TransactionReference,
            TransactionDateTime = t.TransactionDateTime,
        }).ToList();
    }
}