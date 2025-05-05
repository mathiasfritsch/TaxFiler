using TaxFiler.Model.Dto;
using TransactionDto = TaxFiler.Model.Csv.TransactionDto;

namespace TaxFiler.Service;

public interface ITransactionDocumentMatcherService
{
    public DocumentDto[] MatchTransactionToDocument(TransactionDto transaction);
}