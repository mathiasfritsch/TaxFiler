using Refit;

namespace TaxFiler.Service.LlamaClient;

public interface ILlamaApiClient
{
    [Multipart]
    [Post("/api/v1/files")]
    Task<string> UploadFileForParsingAsync([AliasAs("file")] StreamPart file);


    [Get("/api/v1/extraction/extraction-agents?project_id=c22f5d97-22f5-40ab-8992-e40e32b0992c")]
    Task<string> GetAgents();
}