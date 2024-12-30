namespace TaxFiler.Model.Dto;

public class DocumentDto
{
    public required string Name { get; init; }
    public int Id { get; init; } 
    public required string ExternalRef { get; init; }
    public bool Orphaned { get; init; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Total { get; set; }
    public decimal? SubTotal { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool Parsed { get; set; }
    public int TaxMonth { get; set; }
    public int TaxYear { get; set; }
    public decimal? Skonto { get; set; }
}