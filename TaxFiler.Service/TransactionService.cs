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

    public async Task AddTransactionsAsync(IEnumerable<TransactionDto> transactions)
    {
        foreach (var transaction in transactions) 
            taxFilerContext.Transactions.Add(transaction.ToTransaction());
        
        await taxFilerContext.SaveChangesAsync();
    }
    
    public async Task TruncateTransactionsAsync()
        => await taxFilerContext.Transactions.ExecuteDeleteAsync( );

    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync()
    {
        var transactions = await taxFilerContext.Transactions.ToListAsync();
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
        TransactionMapper.UpdateTransaction(transaction, transactionDto);
        await taxFilerContext.SaveChangesAsync();
    }
}