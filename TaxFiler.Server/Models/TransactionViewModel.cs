using TaxFiler.Model.Dto;

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
    public bool IsSalesTaxRelevant { get; init; }
    public int AccountId { get; init; }
    public string AccountName { get; init; }
    public bool IsTaxMismatch { get; init; }
    
    // Multiple document attachment support
    /// <summary>
    /// Collection of all documents attached to this transaction
    /// </summary>
    public IEnumerable<DocumentDto> AttachedDocuments { get; init; } = new List<DocumentDto>();
    
    /// <summary>
    /// Total number of documents attached to this transaction
    /// </summary>
    public int AttachedDocumentCount { get; init; }
    
    /// <summary>
    /// Sum of all attached document amounts
    /// </summary>
    public decimal TotalAttachedAmount { get; init; }
    
    /// <summary>
    /// Indicates whether there's a mismatch between transaction amount and total attached document amounts
    /// </summary>
    public bool HasAttachmentAmountMismatch { get; init; }
}