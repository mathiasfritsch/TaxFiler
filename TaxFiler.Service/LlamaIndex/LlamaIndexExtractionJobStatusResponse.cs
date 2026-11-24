namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexExtractionJobStatusResponse
{
    public required string id { get; set; }

    public required string status { get; set; }
    // Add other fields as needed based on the API response
}