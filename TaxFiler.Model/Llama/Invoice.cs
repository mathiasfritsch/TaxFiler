namespace TaxFiler.Model.Llama;

public class Invoice
{
    public required string InvoiceNumber { get; init; }
    public required DateOnly InvoiceDate { get; init; }
    public decimal SubTotal { get; init; }
    public decimal Total { get; init; }
    public required string Status { get; init; }
    public required Tax Tax { get; init; }
}