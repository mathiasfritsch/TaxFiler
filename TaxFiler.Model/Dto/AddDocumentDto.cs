namespace TaxFiler.Model.Dto;

public class AddDocumentDto
{
    public string? Name { get; init; }
    public string? ExternalRef { get; init; }
    public bool Orphaned { get; init; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Total { get; set; }
    public decimal? SubTotal { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool Parsed { get; set; }
    public decimal Skonto { get; set; }
}