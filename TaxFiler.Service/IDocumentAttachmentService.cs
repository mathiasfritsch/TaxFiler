using FluentResults;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

/// <summary>
/// Service interface for managing document attachments to transactions.
/// Supports multiple documents being attached to a single transaction with validation and audit trail.
/// </summary>
public interface IDocumentAttachmentService
{
    /// <summary>
    /// Retrieves all documents attached to a specific transaction.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get attachments for</param>
    /// <returns>Result containing the list of attached documents, or failure if transaction not found</returns>
    Task<Result<IEnumerable<DocumentDto>>> GetAttachedDocumentsAsync(int transactionId);
    
    /// <summary>
    /// Retrieves documents attached to a specific transaction with pagination support.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get attachments for</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Result containing the paginated list of attached documents</returns>
    Task<Result<PaginatedResult<DocumentDto>>> GetAttachedDocumentsPaginatedAsync(int transactionId, int pageNumber = 1, int pageSize = 50);
    
    /// <summary>
    /// Attaches a document to a transaction.
    /// Validates that both the transaction and document exist and that the attachment doesn't already exist.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to attach the document to</param>
    /// <param name="documentId">The ID of the document to attach</param>
    /// <param name="isAutomatic">Whether this attachment was created automatically by the matching algorithm</param>
    /// <param name="attachedBy">User identifier who performed the attachment (optional)</param>
    /// <returns>Result indicating success or failure with appropriate error messages</returns>
    Task<Result> AttachDocumentAsync(int transactionId, int documentId, bool isAutomatic = false, string? attachedBy = null);
    
    /// <summary>
    /// Removes a document attachment from a transaction.
    /// Only removes the attachment relationship, does not delete the document or transaction.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to detach the document from</param>
    /// <param name="documentId">The ID of the document to detach</param>
    /// <returns>Result indicating success or failure with appropriate error messages</returns>
    Task<Result> DetachDocumentAsync(int transactionId, int documentId);
    
    /// <summary>
    /// Gets a summary of all attachments for a transaction including amount calculations and mismatch detection.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get the summary for</param>
    /// <returns>Result containing attachment summary with amounts and mismatch information</returns>
    Task<Result<AttachmentSummaryDto>> GetAttachmentSummaryAsync(int transactionId);
    
    /// <summary>
    /// Gets the complete attachment history for a transaction including audit information.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get attachment history for</param>
    /// <returns>Result containing the list of document attachments with audit trail information</returns>
    Task<Result<IEnumerable<DocumentAttachment>>> GetAttachmentHistoryAsync(int transactionId);
}