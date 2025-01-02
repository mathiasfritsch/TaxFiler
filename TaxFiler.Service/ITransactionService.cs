using csvModel = TaxFiler.Model.Csv;
using  dtoModel = TaxFiler.Model.Dto;
namespace TaxFiler.Service;

public interface ITransactionService
{
    public IEnumerable<csvModel.TransactionDto> ParseTransactions(TextReader reader);
    public Task AddTransactionsAsync(IEnumerable<csvModel.TransactionDto> transactions, DateTime yearMonth);    
    public Task TruncateTransactionsAsync();
    Task<IEnumerable<dtoModel.TransactionDto>> GetTransactionsAsync();
    Task<IEnumerable<dtoModel.TransactionDto>> GetTransactionsAsync(DateTime yearMonth);
    Task<dtoModel.TransactionDto> GetTransactionAsync(int transactionid);
    Task UpdateTransactionAsync(dtoModel.TransactionDto transactionDto);
    Task DeleteTransactionsAsync(DateTime yearMonth);
    Task<MemoryStream> CreateCsvFileAsync(DateTime yearMonthh);
}