using FluentResults;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public interface IDocumentService
{
    public Task DeleteAllDocumentsAsync();
    public Task<IEnumerable<DocumentDto>> GetDocumentsAsync(DateOnly yearMonth);
    public Task<Result<DocumentDto>> AddDocumentAsync(AddDocumentDto documentDto);
    public Task<Result> UpdateDocumentAsync(int id, DocumentDto documentDto);
    public Task<Result<DocumentDto>> GetDocumentAsync(int id);
    public Task<Result> DeleteDocumentAsync(int id);
    public Task<IEnumerable<DocumentDto>> GetDocumentsByMonthAsync(DateTime yearMonth);
}