namespace TaxFiler.Service.LlamaIndex;

public class InvoiceResult
{
    public string InvoiceNumber { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Total { get; set; }
    public decimal SubTotal { get; set; }
    public string InvoiceDate { get; set; }
    public decimal? Skonto { get; set; }
}