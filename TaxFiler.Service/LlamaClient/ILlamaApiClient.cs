using Refit;
using TaxFiler.Service.LlamaIndex;

namespace TaxFiler.Service.LlamaClient;

public interface ILlamaApiClient
{
    [Multipart]
    [Post("/api/v1/files")]
    Task<LlamaIndexUploadFileResponse> UploadFileAsync([AliasAs("upload_file")] StreamPart file);

    [Post("/api/v1/extraction/jobs")]
    Task<LlamaIndexExtractionJobCreationResponse> CreateExtractionJobAsync([Body] object payload);

    [Get("/api/v1/extraction/jobs/{jobId}")]
    Task<LlamaIndexExtractionJobStatusResponse> GetExtractionJobAsync(string jobId);

    [Get("/api/v1/extraction/jobs/{jobId}/result")]
    Task<LlamaIndexJobResultResponse> GetExtractionJobResultAsync(string jobId);

    [Get("/api/v1/extraction/extraction-agents?project_id=c22f5d97-22f5-40ab-8992-e40e32b0992c")]
    Task<string> GetAgents();
}