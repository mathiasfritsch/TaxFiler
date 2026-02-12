namespace TaxFiler.Model.Dto;

public class UpdateTransactionDto
{
    public decimal GrossAmount { get; init; }
    public required string Counterparty { get; init; }
    public required string TransactionNote { get; init; }
    public required string TransactionReference { get; init; }
    public DateTime TransactionDateTime { get; init; }
    public int Id { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsOutgoing { get; set; }
    public bool IsIncomeTaxRelevant { get; set; }
    public int? DocumentId { get; set; }
    public bool IsSalesTaxRelevant { get; set; }
    public required string SenderReceiver { get; set; }
    public int? AccountId { get; set; }
    public bool IsTaxMismatchConfirmed { get; set; }
}