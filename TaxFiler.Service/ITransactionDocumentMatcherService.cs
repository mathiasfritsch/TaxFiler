using TaxFiler.Model.Dto;
using TransactionDto = TaxFiler.Model.Csv.TransactionDto;

namespace TaxFiler.Service;

public interface ITransactionDocumentMatcherService
{
    public Task<DocumentDto?> MatchTransactionToDocumentAsync(TransactionDto transaction);
    public Task<DocumentDto[]> GetAllUnmatchedDocumentsAsync();
}