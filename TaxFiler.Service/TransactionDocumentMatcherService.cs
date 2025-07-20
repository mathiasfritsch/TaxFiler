using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.Model.Dto;
using TransactionDto = TaxFiler.Model.Csv.TransactionDto;

namespace TaxFiler.Service;
public class TransactionDocumentMatcherService(IDocumentService documentService):ITransactionDocumentMatcherService
{
    public async Task<DocumentDto?> MatchTransactionToDocumentAsync(TransactionDto transaction)
    {
        var unmatchedDocuments = await documentService.GetAllUnmatchedDocumentsAsync();

        return unmatchedDocuments[0];
    }

    public async Task<DocumentDto[]> GetAllUnmatchedDocumentsAsync()
    {
        return await documentService.GetAllUnmatchedDocumentsAsync();
    }
}