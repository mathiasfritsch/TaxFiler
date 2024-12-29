namespace TaxFiler.Model.Dto;

public class TransactionDto
{
    public decimal GrossAmount { get; init; }
    public string Counterparty { get; init; }
    public string TransactionNote { get; init; }
    public string TransactionReference { get; init; }
    public DateTime TransactionDateTime { get; init; }
    public int Id { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
}