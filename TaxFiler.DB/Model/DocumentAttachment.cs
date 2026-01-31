using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxFiler.DB.Model;

/// <summary>
/// Junction entity that manages many-to-many relationships between transactions and documents.
/// Supports multiple documents being attached to a single transaction with audit trail information.
/// </summary>
public class DocumentAttachment
{
    /// <summary>
    /// Primary key for the document attachment relationship
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key reference to the transaction
    /// </summary>
    [ForeignKey(nameof(Transaction))]
    public int TransactionId { get; set; }
    
    /// <summary>
    /// Navigation property to the associated transaction
    /// </summary>
    public Transaction Transaction { get; set; } = null!;
    
    /// <summary>
    /// Foreign key reference to the document
    /// </summary>
    [ForeignKey(nameof(Document))]
    public int DocumentId { get; set; }
    
    /// <summary>
    /// Navigation property to the associated document
    /// </summary>
    public Document Document { get; set; } = null!;
    
    /// <summary>
    /// Timestamp when the document was attached to the transaction
    /// </summary>
    public DateTime AttachedAt { get; set; }
    
    /// <summary>
    /// User identifier who performed the attachment (for audit trail)
    /// Can be null for system-generated attachments
    /// </summary>
    [MaxLength(100)]
    public string? AttachedBy { get; set; }
    
    /// <summary>
    /// Indicates whether the attachment was created automatically by the matching algorithm
    /// or manually by a user
    /// </summary>
    public bool IsAutomatic { get; set; }
}