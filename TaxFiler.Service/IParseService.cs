using FluentResults;
using TaxFiler.Model.Llama;
using TaxFiler.Service.LlamaIndex;

namespace TaxFiler.Service;

public interface IParseService
{
    public Task<Result<LlamaIndexJobResultResponse>> ParseFilesAsync(int documentId);
}