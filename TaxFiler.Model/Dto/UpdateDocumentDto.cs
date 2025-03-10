namespace TaxFiler.Model.Dto;

public class UpdateDocumentDto
{
    public required string Name { get; init; }
    public int Id { get; init; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Total { get; set; }
    public decimal? SubTotal { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool Parsed { get; set; }
    public decimal? Skonto { get; set; }
}