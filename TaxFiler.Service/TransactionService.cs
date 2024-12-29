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
                    TransactionDateTime = new DateTime(transaction.BookingDate,transaction.TimeCompleted),
                    IsOutgoing = transaction.Amount < 0,
                    IsIncomeTaxRelevant = false,
                    IsSalesTaxRelevant = false
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
            IsSalesTaxRelevant = t.IsSalesTaxRelevant,
            IsOutgoing = t.IsOutgoing,
            IsIncomeTaxRelevant = t.IsIncomeTaxRelevant,
            TaxMonth = t.TaxMonth,
            TaxYear = t.TaxYear,
            DocumentId = t.DocumentId
        }).ToList();
    }
    
    public async Task<modelDto.TransactionDto> GetTransactionAsync(int transactionId)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == transactionId);
        return new modelDto.TransactionDto
        {
            Id = transaction.Id,
            NetAmount = transaction.NetAmount,
            GrossAmount = transaction.GrossAmount,
            TaxAmount = transaction.TaxAmount,
            TaxRate = transaction.TaxRate,
            Counterparty = transaction.Counterparty,
            TransactionNote = transaction.TransactionNote,
            TransactionReference = transaction.TransactionReference,
            TransactionDateTime = transaction.TransactionDateTime,
            IsSalesTaxRelevant = transaction.IsSalesTaxRelevant,
            IsOutgoing = transaction.IsOutgoing,
            IsIncomeTaxRelevant = transaction.IsIncomeTaxRelevant
        };
    }
    
    public async Task UpdateTransactionAsync(modelDto.TransactionDto transactionDto)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == transactionDto.Id);
        
        transaction.IsIncomeTaxRelevant = transactionDto.IsIncomeTaxRelevant;
        transaction.IsOutgoing  = transactionDto.IsOutgoing;
        transaction.IsSalesTaxRelevant = transactionDto.IsSalesTaxRelevant;
        transaction.NetAmount = transactionDto.NetAmount;
        transaction.TaxAmount = transactionDto.TaxAmount;
        transaction.TaxRate = transactionDto.TaxRate;
        transaction.TransactionDateTime = transactionDto.TransactionDateTime;
        transaction.TransactionNote = transactionDto.TransactionNote;
        transaction.TransactionReference = transactionDto.TransactionReference;
        transaction.Counterparty = transactionDto.Counterparty;
        
        await taxFilerContext.SaveChangesAsync();
    }
}