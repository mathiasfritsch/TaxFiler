using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
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

    public async Task AddTransactionsAsync(IEnumerable<TransactionDto> transactions, DateTime yearMonth)
    {
        foreach (var transaction in transactions)
        {
            var transactionDb = transaction.ToTransaction();
            transactionDb.TaxYear = yearMonth.Year;
            transactionDb.TaxMonth = yearMonth.Month;
            transactionDb.IsOutgoing = transactionDb.GrossAmount < 0;
            transactionDb.GrossAmount = Math.Abs(transactionDb.GrossAmount);
            
            taxFilerContext.Transactions.Add(transactionDb);
        }   
        
        await taxFilerContext.SaveChangesAsync();
    }
    
    public async Task TruncateTransactionsAsync()
        => await taxFilerContext.Transactions.ExecuteDeleteAsync( );

    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync()
    {
        var transactions = await taxFilerContext.Transactions.Include( t => t.Document).ToListAsync();
        return transactions.Select(t => t.TransactionDto()).ToList();
    }
    
    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync(DateTime yearMonth)
    {
        var transactions = await taxFilerContext
            .Transactions
            .Include(t => t.Document)
            .Where( t => t.TaxYear == yearMonth.Year && t.TaxMonth == yearMonth.Month)
            .ToListAsync();
        
        return transactions.Select(t => t.TransactionDto()).ToList();
    }
    
    public async Task<modelDto.TransactionDto> GetTransactionAsync(int transactionId)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == transactionId);
        return transaction.TransactionDto();
    }
    
    public async Task UpdateTransactionAsync(modelDto.TransactionDto transactionDto)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == transactionDto.Id);
        
        if(transaction.DocumentId != transactionDto.DocumentId && transactionDto.DocumentId > 0)
        {
            var document = await taxFilerContext.Documents.SingleAsync(d => d.Id == transactionDto.DocumentId);
            transactionDto.NetAmount = document.SubTotal.GetValueOrDefault();
            transactionDto.TaxAmount = document.TaxAmount.GetValueOrDefault();
            transactionDto.TaxRate = document.TaxRate.GetValueOrDefault();
        }
        
        TransactionMapper.UpdateTransaction(transaction, transactionDto);
        await taxFilerContext.SaveChangesAsync();
    }

    public async Task DeleteTransactionsAsync(DateTime yearMonth)
        => await taxFilerContext
            .Transactions
            .Where(t => t.TaxYear == yearMonth.Year && t.TaxMonth == yearMonth.Month)
            .ExecuteDeleteAsync( );
    
}