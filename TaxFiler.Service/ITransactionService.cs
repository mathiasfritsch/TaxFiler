using csvModel = TaxFiler.Model.Csv;
using  dtoModel = TaxFiler.Model.Dto;
namespace TaxFiler.Service;

public interface ITransactionService
{
    public IEnumerable<csvModel.TransactionDto> ParseTransactions(TextReader reader);
    public Task AddTransactionsAsync(IEnumerable<csvModel.TransactionDto> transactions);
    public Task TruncateTransactionsAsync();
    Task<IEnumerable<dtoModel.TransactionDto>> GetTransactionsAsync();
    Task<dtoModel.TransactionDto> GetTransactionAsync(int transactionid);
    Task UpdateTransactionAsync(dtoModel.TransactionDto transactionDto);
}