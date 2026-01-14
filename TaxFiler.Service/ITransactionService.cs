using csvModel = TaxFiler.Model.Csv;
using  dtoModel = TaxFiler.Model.Dto;
namespace TaxFiler.Service;

public interface ITransactionService
{
    public IEnumerable<csvModel.TransactionDto> ParseTransactions(TextReader reader);
    public Task AddTransactionsAsync(IEnumerable<csvModel.TransactionDto> transactions);    
    Task<IEnumerable<dtoModel.TransactionDto>> GetTransactionsAsync(DateOnly yearMonth, int? accountId = null);
    Task<dtoModel.TransactionDto> GetTransactionAsync(int transactionid);
    Task UpdateTransactionAsync(dtoModel.UpdateTransactionDto transactionDto);
    Task<MemoryStream> CreateCsvFileAsync(DateOnly yearMonthh);
    Task DeleteTransactionAsync(int id);
    
    /// <summary>
    /// Auto-assigns documents to unmatched transactions in a given month.
    /// </summary>
    /// <param name="yearMonth">The year-month to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result summary with counts of processed, assigned, and skipped transactions</returns>
    Task<dtoModel.AutoAssignResult> AutoAssignDocumentsAsync(DateOnly yearMonth, CancellationToken cancellationToken = default);
}