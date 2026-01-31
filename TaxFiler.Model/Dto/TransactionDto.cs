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
    public bool IsOutgoing { get; set; }
    public bool IsIncomeTaxRelevant { get; set; }
    
    // Multiple document attachment support
    /// <summary>
    /// Collection of all documents attached to this transaction
    /// </summary>
    public IEnumerable<DocumentDto> AttachedDocuments { get; set; } = new List<DocumentDto>();
    
    /// <summary>
    /// Total number of documents attached to this transaction
    /// </summary>
    public int AttachedDocumentCount { get; set; }
    
    /// <summary>
    /// Sum of all attached document amounts
    /// </summary>
    public decimal TotalAttachedAmount { get; set; }
    
    /// <summary>
    /// Indicates whether there's a mismatch between transaction amount and total attached document amounts
    /// </summary>
    public bool HasAttachmentAmountMismatch { get; set; }
    
    public bool IsSalesTaxRelevant { get; set; }
    public string SenderReceiver { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; }
    public bool IsTaxMismatch { get; set; }
}