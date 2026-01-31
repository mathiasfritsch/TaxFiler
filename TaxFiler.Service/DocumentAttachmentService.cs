using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

/// <summary>
/// Service for managing document attachments to transactions.
/// Supports multiple documents being attached to a single transaction with validation and audit trail.
/// </summary>
public class DocumentAttachmentService : IDocumentAttachmentService
{
    private readonly TaxFilerContext _context;
    private readonly ILogger<DocumentAttachmentService> _logger;
    private const decimal AmountMismatchThreshold = 0.01m; // 1 cent tolerance for amount comparisons
    private const decimal AmountWarningThreshold = 1.10m; // Warn if attached documents exceed transaction by 10%

    public DocumentAttachmentService(TaxFilerContext context, ILogger<DocumentAttachmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResult<DocumentDto>>> GetAttachedDocumentsPaginatedAsync(int transactionId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
            {
                return Result.Fail("Page number must be greater than 0");
            }
            
            if (pageSize < 1 || pageSize > 1000)
            {
                return Result.Fail("Page size must be between 1 and 1000");
            }

            // Get total count first
            var totalCount = await _context.DocumentAttachments
                .Where(da => da.TransactionId == transactionId)
                .CountAsync();

            if (totalCount == 0)
            {
                // Check if transaction exists
                var transactionExists = await _context.Transactions
                    .AnyAsync(t => t.Id == transactionId);
                
                if (!transactionExists)
                {
                    _logger.LogWarning("Attempted to get paginated attachments for non-existent transaction {TransactionId}", transactionId);
                    return Result.Fail($"Transaction with ID {transactionId} not found");
                }

                // Transaction exists but has no attachments
                return Result.Ok(new PaginatedResult<DocumentDto>(
                    new List<DocumentDto>(), pageNumber, pageSize, 0));
            }

            // Get paginated documents with optimized query
            var attachedDocuments = await _context.DocumentAttachments
                .Where(da => da.TransactionId == transactionId)
                .Include(da => da.Document)
                .OrderBy(da => da.AttachedAt) // Consistent ordering for pagination
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(da => da.Document)
                .ToListAsync();

            // Convert to DTOs
            var documentDtos = attachedDocuments
                .Select(doc => doc.ToDto(new int?[] { doc.Id }))
                .ToList();

            var result = new PaginatedResult<DocumentDto>(documentDtos, pageNumber, pageSize, totalCount);

            _logger.LogInformation("Retrieved page {PageNumber} of attached documents for transaction {TransactionId}: {Count}/{TotalCount} documents", 
                pageNumber, transactionId, documentDtos.Count, totalCount);

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated attached documents for transaction {TransactionId}", transactionId);
            return Result.Fail($"Error retrieving paginated attached documents: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<DocumentDto>>> GetAttachedDocumentsAsync(int transactionId)
    {
        try
        {
            // Optimized query: Get transaction with all attached documents in a single query
            var transaction = await _context.Transactions
                .Where(t => t.Id == transactionId)
                .Include(t => t.DocumentAttachments)
                .ThenInclude(da => da.Document)
                .FirstOrDefaultAsync();
            
            if (transaction == null)
            {
                _logger.LogWarning("Attempted to get attachments for non-existent transaction {TransactionId}", transactionId);
                return Result.Fail($"Transaction with ID {transactionId} not found");
            }

            // Convert to DTOs - for attached documents, they are not unconnected
            var documentDtos = transaction.DocumentAttachments
                .Select(da => da.Document.ToDto(new int?[] { da.Document.Id }))
                .ToList();

            _logger.LogInformation("Retrieved {Count} attached documents for transaction {TransactionId}", 
                documentDtos.Count, transactionId);

            return Result.Ok<IEnumerable<DocumentDto>>(documentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attached documents for transaction {TransactionId}", transactionId);
            return Result.Fail($"Error retrieving attached documents: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> AttachDocumentAsync(int transactionId, int documentId, bool isAutomatic = false, string? attachedBy = null)
    {
        try
        {
            // Verify transaction exists
            var transaction = await _context.Transactions
                .Include(t => t.DocumentAttachments)
                .ThenInclude(da => da.Document)
                .FirstOrDefaultAsync(t => t.Id == transactionId);
            
            if (transaction == null)
            {
                _logger.LogWarning("Attempted to attach document {DocumentId} to non-existent transaction {TransactionId}", 
                    documentId, transactionId);
                return Result.Fail($"Transaction with ID {transactionId} not found");
            }

            // Verify document exists
            var document = await _context.Documents
                .Include(d => d.DocumentAttachments)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            
            if (document == null)
            {
                _logger.LogWarning("Attempted to attach non-existent document {DocumentId} to transaction {TransactionId}", 
                    documentId, transactionId);
                return Result.Fail($"Document with ID {documentId} not found");
            }

            // Business Rule 5.1: Prevent duplicate attachments
            var existingAttachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(da => da.TransactionId == transactionId && da.DocumentId == documentId);
            
            if (existingAttachment != null)
            {
                _logger.LogWarning("Attempted to create duplicate attachment: Document {DocumentId} already attached to transaction {TransactionId}", 
                    documentId, transactionId);
                return Result.Fail($"Document {documentId} is already attached to transaction {transactionId}");
            }

            // Business Rule 5.4: Check if document is already attached to another transaction (warning only)
            var otherAttachments = document.DocumentAttachments
                .Where(da => da.TransactionId != transactionId)
                .ToList();

            var warnings = new List<string>();
            if (otherAttachments.Any())
            {
                var otherTransactionIds = string.Join(", ", otherAttachments.Select(da => da.TransactionId));
                warnings.Add($"Document {documentId} is already attached to other transaction(s): {otherTransactionIds}");
                _logger.LogInformation("Document {DocumentId} being attached to transaction {TransactionId} is already attached to other transactions: {OtherTransactions}", 
                    documentId, transactionId, otherTransactionIds);
            }

            // Business Rule 5.2: Amount validation with warning
            if (document.Total.HasValue)
            {
                var currentTotalAttached = transaction.DocumentAttachments
                    .Where(da => da.Document.Total.HasValue)
                    .Sum(da => da.Document.Total!.Value);
                
                var newTotalAttached = currentTotalAttached + document.Total.Value;
                var transactionAmount = Math.Abs(transaction.GrossAmount);
                
                if (newTotalAttached > transactionAmount * AmountWarningThreshold)
                {
                    var overage = newTotalAttached - transactionAmount;
                    warnings.Add($"Total attached document amount ({newTotalAttached:C}) will exceed transaction amount ({transactionAmount:C}) by {overage:C}");
                    _logger.LogWarning("Amount overage warning for transaction {TransactionId}: Total attached {TotalAttached:C} exceeds transaction amount {TransactionAmount:C} by {Overage:C}", 
                        transactionId, newTotalAttached, transactionAmount, overage);
                }
            }

            // Create new attachment
            var attachment = new DocumentAttachment
            {
                TransactionId = transactionId,
                DocumentId = documentId,
                AttachedAt = DateTime.UtcNow,
                AttachedBy = attachedBy,
                IsAutomatic = isAutomatic
            };

            _context.DocumentAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            // Business Rule 5.5: Audit trail logging
            _logger.LogInformation("Document attachment created: Document {DocumentId} attached to transaction {TransactionId} by {AttachedBy} (Automatic: {IsAutomatic})", 
                documentId, transactionId, attachedBy ?? "Unknown", isAutomatic);

            // Return success with warnings if any
            var result = Result.Ok();
            if (warnings.Any())
            {
                foreach (var warning in warnings)
                {
                    result.WithSuccess(warning);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching document {DocumentId} to transaction {TransactionId}", documentId, transactionId);
            return Result.Fail($"Error attaching document: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DetachDocumentAsync(int transactionId, int documentId)
    {
        try
        {
            // Find the attachment
            var attachment = await _context.DocumentAttachments
                .FirstOrDefaultAsync(da => da.TransactionId == transactionId && da.DocumentId == documentId);
            
            if (attachment == null)
            {
                _logger.LogWarning("Attempted to detach non-existent attachment: Document {DocumentId} from transaction {TransactionId}", 
                    documentId, transactionId);
                return Result.Fail($"No attachment found between transaction {transactionId} and document {documentId}");
            }

            // Remove the attachment
            _context.DocumentAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            // Business Rule 5.5: Audit trail logging
            _logger.LogInformation("Document attachment removed: Document {DocumentId} detached from transaction {TransactionId} (was attached by {AttachedBy} on {AttachedAt})", 
                documentId, transactionId, attachment.AttachedBy ?? "Unknown", attachment.AttachedAt);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching document {DocumentId} from transaction {TransactionId}", documentId, transactionId);
            return Result.Fail($"Error detaching document: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AttachmentSummaryDto>> GetAttachmentSummaryAsync(int transactionId)
    {
        try
        {
            // Get transaction with its attachments
            var transaction = await _context.Transactions
                .Include(t => t.DocumentAttachments)
                .ThenInclude(da => da.Document)
                .FirstOrDefaultAsync(t => t.Id == transactionId);
            
            if (transaction == null)
            {
                _logger.LogWarning("Attempted to get attachment summary for non-existent transaction {TransactionId}", transactionId);
                return Result.Fail($"Transaction with ID {transactionId} not found");
            }

            // Calculate summary information
            var attachedDocuments = transaction.DocumentAttachments
                .Select(da => da.Document.ToDto(new int?[] { da.Document.Id }))
                .ToList();

            var totalAttachedAmount = transaction.DocumentAttachments
                .Where(da => da.Document.Total.HasValue)
                .Sum(da => da.Document.Total!.Value);

            var transactionAmount = Math.Abs(transaction.GrossAmount);
            var amountDifference = totalAttachedAmount - transactionAmount;
            var hasAmountMismatch = Math.Abs(amountDifference) > AmountMismatchThreshold;

            var summary = new AttachmentSummaryDto
            {
                TransactionId = transactionId,
                AttachedDocumentCount = transaction.DocumentAttachments.Count,
                TotalAttachedAmount = totalAttachedAmount,
                TransactionAmount = transactionAmount,
                AmountDifference = amountDifference,
                HasAmountMismatch = hasAmountMismatch,
                AttachedDocuments = attachedDocuments
            };

            _logger.LogDebug("Generated attachment summary for transaction {TransactionId}: {AttachedCount} documents, {TotalAmount:C} total, mismatch: {HasMismatch}", 
                transactionId, summary.AttachedDocumentCount, summary.TotalAttachedAmount, summary.HasAmountMismatch);

            return Result.Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachment summary for transaction {TransactionId}", transactionId);
            return Result.Fail($"Error getting attachment summary: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<DocumentAttachment>>> GetAttachmentHistoryAsync(int transactionId)
    {
        try
        {
            // Verify transaction exists
            var transactionExists = await _context.Transactions
                .AnyAsync(t => t.Id == transactionId);
            
            if (!transactionExists)
            {
                _logger.LogWarning("Attempted to get attachment history for non-existent transaction {TransactionId}", transactionId);
                return Result.Fail($"Transaction with ID {transactionId} not found");
            }

            // Get attachment history with document information
            var attachments = await _context.DocumentAttachments
                .Where(da => da.TransactionId == transactionId)
                .Include(da => da.Document)
                .Include(da => da.Transaction)
                .OrderBy(da => da.AttachedAt)
                .ToListAsync();

            _logger.LogDebug("Retrieved attachment history for transaction {TransactionId}: {Count} attachments", 
                transactionId, attachments.Count);

            return Result.Ok<IEnumerable<DocumentAttachment>>(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachment history for transaction {TransactionId}", transactionId);
            return Result.Fail($"Error retrieving attachment history: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates user permissions for document attachment operations.
    /// Business Rule 5.3: User permission validation.
    /// </summary>
    /// <param name="userId">The user ID to validate permissions for</param>
    /// <param name="documentId">The document ID to check access for</param>
    /// <returns>True if user has access, false otherwise</returns>
    public async Task<bool> ValidateUserPermissionsAsync(string userId, int documentId)
    {
        try
        {
            // For now, implement basic validation - in a real system this would check
            // user roles, document ownership, or other permission mechanisms
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                _logger.LogWarning("Permission validation failed: Document {DocumentId} not found", documentId);
                return false;
            }

            // TODO: Implement actual permission logic based on business requirements
            // This could involve checking:
            // - Document ownership
            // - User roles and permissions
            // - Account-based access control
            // - etc.

            _logger.LogDebug("User {UserId} permission validated for document {DocumentId}", userId, documentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user permissions for user {UserId} and document {DocumentId}", userId, documentId);
            return false;
        }
    }
}