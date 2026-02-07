namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexExtractionJobResponse
{
    public required string id { get; set; }
    public JobStatus status { get; set; }
    public required string extraction_agent_id { get; set; }
    public required string file_id { get; set; }
    public required string created_at { get; set; }
    public required string updated_at { get; set; }
    // Add other fields as needed based on the API response
}