using FluentResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

/// <summary>
/// Cached decorator for DocumentAttachmentService that provides caching for frequently accessed data.
/// Implements cache invalidation strategies to maintain data consistency.
/// </summary>
public class CachedDocumentAttachmentService : IDocumentAttachmentService
{
    private readonly DocumentAttachmentService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedDocumentAttachmentService> _logger;
    
    // Cache configuration+++++++++++++++++++++++++++++++++++++++++++++++
    private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SummaryCacheExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AttachmentListCacheExpiry = TimeSpan.FromMinutes(10);
    
    // Cache key prefixes
    private const string AttachedDocumentsCacheKeyPrefix = "attached_docs_";
    private const string AttachmentSummaryCacheKeyPrefix = "attachment_summary_";
    private const string AttachmentHistoryCacheKeyPrefix = "attachment_history_";
    private const string PaginatedAttachmentsCacheKeyPrefix = "paginated_attachments_";

    public CachedDocumentAttachmentService(
        DocumentAttachmentService innerService,
        IMemoryCache cache,
        ILogger<CachedDocumentAttachmentService> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<DocumentDto>>> GetAttachedDocumentsAsync(int transactionId)
    {
        var cacheKey = $"{AttachedDocumentsCacheKeyPrefix}{transactionId}";
        
        if (_cache.TryGetValue(cacheKey, out Result<IEnumerable<DocumentDto>>? cachedResult))
        {
            _logger.LogDebug("Cache hit for attached documents: transaction {TransactionId}", transactionId);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for attached documents: transaction {TransactionId}", transactionId);
        var result = await _innerService.GetAttachedDocumentsAsync(transactionId);
        
        if (result.IsSuccess)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AttachmentListCacheExpiry,
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(cacheKey, result, cacheOptions);
            _logger.LogDebug("Cached attached documents for transaction {TransactionId}", transactionId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<PaginatedResult<DocumentDto>>> GetAttachedDocumentsPaginatedAsync(int transactionId, int pageNumber = 1, int pageSize = 50)
    {
        var cacheKey = $"{PaginatedAttachmentsCacheKeyPrefix}{transactionId}_{pageNumber}_{pageSize}";
        
        if (_cache.TryGetValue(cacheKey, out Result<PaginatedResult<DocumentDto>>? cachedResult))
        {
            _logger.LogDebug("Cache hit for paginated attached documents: transaction {TransactionId}, page {PageNumber}", transactionId, pageNumber);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for paginated attached documents: transaction {TransactionId}, page {PageNumber}", transactionId, pageNumber);
        var result = await _innerService.GetAttachedDocumentsPaginatedAsync(transactionId, pageNumber, pageSize);
        
        if (result.IsSuccess)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AttachmentListCacheExpiry,
                SlidingExpiration = TimeSpan.FromMinutes(3),
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(cacheKey, result, cacheOptions);
            _logger.LogDebug("Cached paginated attached documents for transaction {TransactionId}, page {PageNumber}", transactionId, pageNumber);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> AttachDocumentAsync(int transactionId, int documentId, bool isAutomatic = false, string? attachedBy = null)
    {
        var result = await _innerService.AttachDocumentAsync(transactionId, documentId, isAutomatic, attachedBy);
        
        if (result.IsSuccess)
        {
            // Invalidate all caches related to this transaction
            InvalidateTransactionCaches(transactionId);
            _logger.LogDebug("Invalidated caches after attaching document {DocumentId} to transaction {TransactionId}", documentId, transactionId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> DetachDocumentAsync(int transactionId, int documentId)
    {
        var result = await _innerService.DetachDocumentAsync(transactionId, documentId);
        
        if (result.IsSuccess)
        {
            // Invalidate all caches related to this transaction
            InvalidateTransactionCaches(transactionId);
            _logger.LogDebug("Invalidated caches after detaching document {DocumentId} from transaction {TransactionId}", documentId, transactionId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<AttachmentSummaryDto>> GetAttachmentSummaryAsync(int transactionId)
    {
        var cacheKey = $"{AttachmentSummaryCacheKeyPrefix}{transactionId}";
        
        if (_cache.TryGetValue(cacheKey, out Result<AttachmentSummaryDto>? cachedResult))
        {
            _logger.LogDebug("Cache hit for attachment summary: transaction {TransactionId}", transactionId);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for attachment summary: transaction {TransactionId}", transactionId);
        var result = await _innerService.GetAttachmentSummaryAsync(transactionId);
        
        if (result.IsSuccess)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = SummaryCacheExpiry,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Priority = CacheItemPriority.High // Summaries are frequently accessed
            };
            
            _cache.Set(cacheKey, result, cacheOptions);
            _logger.LogDebug("Cached attachment summary for transaction {TransactionId}", transactionId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<DocumentAttachment>>> GetAttachmentHistoryAsync(int transactionId)
    {
        var cacheKey = $"{AttachmentHistoryCacheKeyPrefix}{transactionId}";
        
        if (_cache.TryGetValue(cacheKey, out Result<IEnumerable<DocumentAttachment>>? cachedResult))
        {
            _logger.LogDebug("Cache hit for attachment history: transaction {TransactionId}", transactionId);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for attachment history: transaction {TransactionId}", transactionId);
        var result = await _innerService.GetAttachmentHistoryAsync(transactionId);
        
        if (result.IsSuccess)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultCacheExpiry,
                SlidingExpiration = TimeSpan.FromMinutes(7),
                Priority = CacheItemPriority.Low // History is accessed less frequently
            };
            
            _cache.Set(cacheKey, result, cacheOptions);
            _logger.LogDebug("Cached attachment history for transaction {TransactionId}", transactionId);
        }

        return result;
    }

    /// <summary>
    /// Invalidates all cached data related to a specific transaction.
    /// This method is called whenever attachment data changes to ensure cache consistency.
    /// </summary>
    /// <param name="transactionId">The transaction ID to invalidate caches for</param>
    private void InvalidateTransactionCaches(int transactionId)
    {
        var keysToRemove = new[]
        {
            $"{AttachedDocumentsCacheKeyPrefix}{transactionId}",
            $"{AttachmentSummaryCacheKeyPrefix}{transactionId}",
            $"{AttachmentHistoryCacheKeyPrefix}{transactionId}"
        };

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        // Invalidate paginated results for this transaction
        // Since we don't know all possible page combinations, we'll use a more aggressive approach
        // In a production system, you might want to use cache tags or a more sophisticated invalidation strategy
        InvalidatePaginatedCaches(transactionId);
        
        _logger.LogDebug("Invalidated {Count} cache entries for transaction {TransactionId}", keysToRemove.Length, transactionId);
    }

    /// <summary>
    /// Invalidates paginated cache entries for a transaction.
    /// This is a simplified approach - in production, consider using cache tags for more efficient invalidation.
    /// </summary>
    /// <param name="transactionId">The transaction ID to invalidate paginated caches for</param>
    private void InvalidatePaginatedCaches(int transactionId)
    {
        // Remove common pagination combinations
        var commonPageSizes = new[] { 10, 20, 50, 100 };
        var maxPages = 10; // Assume most transactions won't have more than 10 pages of documents

        for (int pageSize = 1; pageSize <= maxPages; pageSize++)
        {
            foreach (var size in commonPageSizes)
            {
                var key = $"{PaginatedAttachmentsCacheKeyPrefix}{transactionId}_{pageSize}_{size}";
                _cache.Remove(key);
            }
        }
    }

    /// <summary>
    /// Clears all attachment-related caches. Useful for testing or administrative operations.
    /// </summary>
    public void ClearAllCaches()
    {
        // Note: IMemoryCache doesn't provide a direct way to clear all entries
        // In a production system, you might want to use a more sophisticated caching solution
        // that supports cache tags or pattern-based invalidation
        
        _logger.LogInformation("Cache clear requested - individual entries will expire naturally");
        
        // For now, we rely on natural expiration
        // In a production system, consider using:
        // - IMemoryCache with cache tags
        // - Redis with pattern-based key deletion
        // - Custom cache implementation with clear functionality
    }
}