namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexUploadFileResponse
{
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string status { get; set; }
    public string created_at { get; set; }
    public string updated_at { get; set; }
    // Add other fields as needed based on the API response
}