namespace TaxFiler.Service.LlamaIndex;

public interface ILlamaIndexService
{
    public Task<LlamaIndexJobResultResponse> UploadFileAndCreateJobAsync(byte[] bytes, string fileName);
}