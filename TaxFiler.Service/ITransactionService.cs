using FluentResults;
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
    Task<MemoryStream> CreateExcelFileAsync(DateOnly yearMonth);
    Task DeleteTransactionAsync(int id);
    
    /// <summary>
    /// Auto-assigns documents to unmatched transactions in a given month.
    /// </summary>
    /// <param name="yearMonth">The year-month to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result summary with counts of processed, assigned, and skipped transactions</returns>
    Task<dtoModel.AutoAssignResult> AutoAssignDocumentsAsync(DateOnly yearMonth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a document to a transaction. A document may be linked to many
    /// transactions and a transaction may have many documents.
    /// </summary>
    Task<Result<dtoModel.TransactionDocumentDto>> AddDocumentToTransactionAsync(
        int transactionId,
        dtoModel.AddDocumentToTransactionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document link from a transaction, leaving both records intact.
    /// </summary>
    Task<Result> RemoveDocumentFromTransactionAsync(
        int transactionId,
        int documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the sequence-ordered documents linked to a transaction.
    /// </summary>
    Task<IEnumerable<dtoModel.TransactionDocumentDto>> GetTransactionDocumentsAsync(
        int transactionId,
        CancellationToken cancellationToken = default);
}