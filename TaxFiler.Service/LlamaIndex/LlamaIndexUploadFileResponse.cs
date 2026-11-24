namespace TaxFiler.Service.LlamaIndex;

/// <summary>
/// Response from LlamaParse upload endpoint
/// The API returns only the job ID and initial status
/// </summary>
public class LlamaIndexUploadFileResponse
{
    public required string Id { get; init; }
}