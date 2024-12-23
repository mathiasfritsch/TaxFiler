using FluentResults;
using TaxFiler.Model.Llama;

namespace TaxFiler.Service;

public interface IParseService
{
    public Task<Result<Invoice>> ParseFilesAsync(int documentId);
}