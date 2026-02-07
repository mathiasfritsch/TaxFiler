namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexUploadFileResponse
{
    public required string id { get; set; }
    public required string name { get; set; }
    public required string type { get; set; }
    public required string status { get; set; }
    public required string created_at { get; set; }
    public required string updated_at { get; set; }
    // Add other fields as needed based on the API response
}