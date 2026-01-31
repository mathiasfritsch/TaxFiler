namespace TaxFiler.Model.Dto;

/// <summary>
/// Summary information about all documents attached to a specific transaction.
/// Provides overview of attachment status, amounts, and potential discrepancies.
/// </summary>
public class AttachmentSummaryDto
{
    /// <summary>
    /// The transaction ID this summary relates to
    /// </summary>
    public int TransactionId { get; set; }
    
    /// <summary>
    /// Total number of documents currently attached to the transaction
    /// </summary>
    public int AttachedDocumentCount { get; set; }
    
    /// <summary>
    /// Sum of all attached document amounts
    /// </summary>
    public decimal TotalAttachedAmount { get; set; }
    
    /// <summary>
    /// The original transaction amount for comparison
    /// </summary>
    public decimal TransactionAmount { get; set; }
    
    /// <summary>
    /// Difference between transaction amount and total attached amount
    /// Positive values indicate attached documents exceed transaction amount
    /// </summary>
    public decimal AmountDifference { get; set; }
    
    /// <summary>
    /// Indicates whether there's a significant mismatch between transaction and attached document amounts
    /// </summary>
    public bool HasAmountMismatch { get; set; }
    
    /// <summary>
    /// List of all documents currently attached to the transaction
    /// </summary>
    public IEnumerable<DocumentDto> AttachedDocuments { get; set; } = new List<DocumentDto>();
}