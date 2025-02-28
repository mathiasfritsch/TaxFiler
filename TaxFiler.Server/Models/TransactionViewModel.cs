namespace TaxFiler.Models;

public class TransactionViewModel
{
    public int Id { get; init; }
    public decimal GrossAmount { get; init; }
    public string? Counterparty { get; init; }
    public string? SenderReceiver { get; init; }
    public string? TransactionNote { get; init; }
    public string? TransactionReference { get; init; }
    public DateTime TransactionDateTime { get; init; }
    public decimal NetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsOutgoing { get; init; }
    public bool IsIncomeTaxRelevant { get; init; }
    public int TaxMonth { get; init; }
    public int TaxYear { get; init; }
    public int? DocumentId { get; init; }
    public bool IsSalesTaxRelevant { get; init; }
    public string? DocumentName { get; init; }
}