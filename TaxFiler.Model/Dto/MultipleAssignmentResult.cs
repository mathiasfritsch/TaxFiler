namespace TaxFiler.Model.Dto;

/// <summary>
/// Result of automatically assigning multiple documents to a single transaction.
/// Provides detailed information about the assignment operation including warnings and attached documents.
/// </summary>
public class MultipleAssignmentResult
{
    /// <summary>
    /// The transaction ID that documents were assigned to
    /// </summary>
    public int TransactionId { get; set; }
    
    /// <summary>
    /// Number of documents successfully attached during the operation
    /// </summary>
    public int DocumentsAttached { get; set; }
    
    /// <summary>
    /// Total amount of all documents that were attached
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Indicates whether the assignment operation generated any warnings
    /// </summary>
    public bool HasWarnings { get; set; }
    
    /// <summary>
    /// List of warning messages generated during the assignment process
    /// Examples: amount mismatches, duplicate attachments, etc.
    /// </summary>
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    
    /// <summary>
    /// List of documents that were successfully attached to the transaction
    /// </summary>
    public IEnumerable<DocumentDto> AttachedDocuments { get; set; } = new List<DocumentDto>();
}