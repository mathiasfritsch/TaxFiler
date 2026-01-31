using System.ComponentModel.DataAnnotations;

namespace TaxFiler.Model.Dto;

/// <summary>
/// Request model for attaching a document to a transaction.
/// Used by the API to specify which document should be attached to which transaction.
/// </summary>
public class AttachDocumentRequestDto
{
    /// <summary>
    /// The ID of the transaction to attach the document to
    /// </summary>
    [Required(ErrorMessage = "Transaction ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Transaction ID must be a positive integer")]
    public int TransactionId { get; set; }
    
    /// <summary>
    /// The ID of the document to attach to the transaction
    /// </summary>
    [Required(ErrorMessage = "Document ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Document ID must be a positive integer")]
    public int DocumentId { get; set; }
}