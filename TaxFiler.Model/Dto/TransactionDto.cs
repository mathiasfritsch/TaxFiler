namespace TaxFiler.Model.Dto;

public class TransactionDto
{
    public decimal GrossAmount { get; init; }
    public required string Counterparty { get; init; }
    public required string TransactionNote { get; init; }
    public required string TransactionReference { get; init; }
    public DateTime TransactionDateTime { get; init; }
    public int Id { get; init; }
    public decimal NetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsOutgoing { get; init; }
    public bool IsIncomeTaxRelevant { get; init; }
    public int? DocumentId { get; init; }
    public DocumentDto? Document { get; init; }
    public bool IsSalesTaxRelevant { get; init; }
    public required string SenderReceiver { get; init; }
    public int AccountId { get; init; }
    public required string AccountName { get; init; }
    public bool IsTaxMismatch { get; init; }
}