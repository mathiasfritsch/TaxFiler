namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexExtractionJobResponse
{
    public string id { get; set; }
    public JobStatus status { get; set; }
    public string extraction_agent_id { get; set; }
    public string file_id { get; set; }
    public string created_at { get; set; }
    public string updated_at { get; set; }
    // Add other fields as needed based on the API response
}