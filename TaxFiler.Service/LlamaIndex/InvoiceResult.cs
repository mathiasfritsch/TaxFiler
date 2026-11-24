namespace TaxFiler.Service.LlamaIndex;

public class InvoiceResult
{
    public required string InvoiceNumber { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Total { get; set; }
    public decimal SubTotal { get; set; }
    public required string InvoiceDate { get; set; }
    public decimal? Skonto { get; set; }
    public MerchantInfo? Merchant { get; set; }
}

public class MerchantInfo
{
    public required string Name { get; set; }
}