namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexJobResultResponse
{
    public required string run_id { get; set; }
    public required InvoiceResult data { get; set; }
}