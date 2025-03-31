using FluentResults;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public interface IDocumentService
{
    public Task DeleteAllDocumentsAsync();
    public Task<IEnumerable<DocumentDto>> GetDocumentsAsync();
    public Task<Result<DocumentDto>> AddDocumentAsync(AddDocumentDto documentDto);
    public Task<Result> UpdateDocumentAsync(int id, UpdateDocumentDto documentDto);
    public Task<Result<DocumentDto>> GetDocumentAsync(int id);
    public Task<Result> DeleteDocumentAsync(int id);
}