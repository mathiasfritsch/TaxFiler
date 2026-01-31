using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Server.Controllers;

/// <summary>
/// Controller for managing document attachments to transactions.
/// Supports multiple documents being attached to a single transaction with validation and audit trail.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentAttachmentsController(
    IDocumentAttachmentService attachmentService,
    IDocumentMatchingService matchingService) : ControllerBase
{
    /// <summary>
    /// Retrieves all documents attached to a specific transaction.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get attachments for</param>
    /// <returns>List of attached documents or error response</returns>
    [HttpGet("transaction/{transactionId}/documents")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<DocumentDto>))]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAttachedDocuments(int transactionId)
    {
        var result = await attachmentService.GetAttachedDocumentsAsync(transactionId);
        
        if (result.IsFailed)
        {
            return NotFound(new { Message = result.Errors.FirstOrDefault()?.Message ?? "Transaction not found" });
        }
        
        return Ok(result.Value);
    }
    
    /// <summary>
    /// Attaches a document to a transaction.
    /// </summary>
    /// <param name="request">The attachment request containing transaction and document IDs</param>
    /// <returns>Success response or error details</returns>
    [HttpPost("attach")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> AttachDocument([FromBody] AttachDocumentRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Get current user for audit trail and permission validation
        var currentUser = User.Identity?.Name ?? "Unknown";
        
        // Business Rule 5.3: Validate user permissions
        var hasPermission = await ((DocumentAttachmentService)attachmentService)
            .ValidateUserPermissionsAsync(currentUser, request.DocumentId);
        
        if (!hasPermission)
        {
            return Forbid("Insufficient permissions to attach this document");
        }
        
        var result = await attachmentService.AttachDocumentAsync(
            request.TransactionId, 
            request.DocumentId, 
            isAutomatic: false, 
            attachedBy: currentUser);
        
        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Message switch
            {
                var msg when msg?.Contains("not found") == true => NotFound(new { Message = msg }),
                var msg when msg?.Contains("already attached") == true => Conflict(new { Message = msg }),
                _ => BadRequest(new { Message = error?.Message ?? "Failed to attach document" })
            };
        }
        
        // Include warnings in response if any
        var warnings = result.Reasons.Select(r => r.Message).ToList();
        var response = new { 
            Message = "Document successfully attached", 
            Warnings = warnings 
        };
        
        return Ok(response);
    }
    
    /// <summary>
    /// Removes a document attachment from a transaction.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to detach the document from</param>
    /// <param name="documentId">The ID of the document to detach</param>
    /// <returns>Success response or error details</returns>
    [HttpDelete("transaction/{transactionId}/document/{documentId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> DetachDocument(int transactionId, int documentId)
    {
        var result = await attachmentService.DetachDocumentAsync(transactionId, documentId);
        
        if (result.IsFailed)
        {
            return NotFound(new { Message = result.Errors.FirstOrDefault()?.Message ?? "Attachment not found" });
        }
        
        return Ok(new { Message = "Document successfully detached" });
    }
    
    /// <summary>
    /// Gets a summary of all attachments for a transaction including amount calculations.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get the summary for</param>
    /// <returns>Attachment summary with amounts and mismatch information</returns>
    [HttpGet("transaction/{transactionId}/summary")]
    [ProducesResponseType(200, Type = typeof(AttachmentSummaryDto))]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AttachmentSummaryDto>> GetAttachmentSummary(int transactionId)
    {
        var result = await attachmentService.GetAttachmentSummaryAsync(transactionId);
        
        if (result.IsFailed)
        {
            return NotFound(new { Message = result.Errors.FirstOrDefault()?.Message ?? "Transaction not found" });
        }
        
        return Ok(result.Value);
    }
    
    /// <summary>
    /// Gets the complete attachment history for a transaction including audit information.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get attachment history for</param>
    /// <returns>List of document attachments with audit trail information</returns>
    [HttpGet("transaction/{transactionId}/history")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<DocumentAttachment>))]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IEnumerable<DocumentAttachment>>> GetAttachmentHistory(int transactionId)
    {
        var result = await attachmentService.GetAttachmentHistoryAsync(transactionId);
        
        if (result.IsFailed)
        {
            return NotFound(new { Message = result.Errors.FirstOrDefault()?.Message ?? "Transaction not found" });
        }
        
        return Ok(result.Value);
    }
    
    /// <summary>
    /// Automatically assigns multiple documents to a transaction based on matching criteria.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to auto-assign documents to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Assignment result with details, warnings, and attached documents</returns>
    [HttpPost("transaction/{transactionId}/auto-assign")]
    [ProducesResponseType(200, Type = typeof(MultipleAssignmentResult))]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<MultipleAssignmentResult>> AutoAssignMultipleDocuments(
        int transactionId, 
        CancellationToken cancellationToken = default)
    {
        var result = await matchingService.AutoAssignMultipleDocumentsAsync(transactionId, cancellationToken);
        
        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Message switch
            {
                var msg when msg?.Contains("not found") == true => NotFound(new { Message = msg }),
                _ => BadRequest(new { Message = error?.Message ?? "Auto-assignment failed" })
            };
        }
        
        return Ok(result.Value);
    }
}