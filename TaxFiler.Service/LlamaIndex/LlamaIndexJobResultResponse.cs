namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexJobResultResponse
{
    public string run_id { get; set; }
    public InvoiceResult data { get; set; }
}