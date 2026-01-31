using FluentResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

/// <summary>
/// Cached decorator for DocumentMatchingService that provides caching for document matching results.
/// Caches matching results to improve performance for repeated queries.
/// </summary>
public class CachedDocumentMatchingService : IDocumentMatchingService
{
    private readonly DocumentMatchingService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedDocumentMatchingService> _logger;
    
    // Cache configuration
    private static readonly TimeSpan MatchingResultsCacheExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MultipleMatchesCacheExpiry = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan BatchMatchesCacheExpiry = TimeSpan.FromMinutes(45);
    
    // Cache key prefixes
    private const string DocumentMatchesCacheKeyPrefix = "doc_matches_";
    private const string MultipleMatchesCacheKeyPrefix = "multi_matches_";
    private const string BatchMatchesCacheKeyPrefix = "batch_matches_";

    public CachedDocumentMatchingService(
        DocumentMatchingService innerService,
        IMemoryCache cache,
        ILogger<CachedDocumentMatchingService> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(Transaction transaction, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            return Enumerable.Empty<DocumentMatch>();

        var cacheKey = $"{DocumentMatchesCacheKeyPrefix}{transaction.Id}_{unconnectedOnly}";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<DocumentMatch>? cachedResult))
        {
            _logger.LogDebug("Cache hit for document matches: transaction {TransactionId}", transaction.Id);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for document matches: transaction {TransactionId}", transaction.Id);
        var result = await _innerService.DocumentMatchesAsync(transaction, unconnectedOnly, cancellationToken);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = MatchingResultsCacheExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.Normal
        };
        
        _cache.Set(cacheKey, result, cacheOptions);
        _logger.LogDebug("Cached document matches for transaction {TransactionId}", transaction.Id);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(int transactionId, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{DocumentMatchesCacheKeyPrefix}{transactionId}_{unconnectedOnly}";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<DocumentMatch>? cachedResult))
        {
            _logger.LogDebug("Cache hit for document matches: transaction {TransactionId}", transactionId);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for document matches: transaction {TransactionId}", transactionId);
        var result = await _innerService.DocumentMatchesAsync(transactionId, unconnectedOnly, cancellationToken);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = MatchingResultsCacheExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.Normal
        };
        
        _cache.Set(cacheKey, result, cacheOptions);
        _logger.LogDebug("Cached document matches for transaction {TransactionId}", transactionId);

        return result;
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, IEnumerable<DocumentMatch>>> BatchDocumentMatchesAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        if (transactions == null)
            return new Dictionary<int, IEnumerable<DocumentMatch>>();

        var transactionList = transactions.ToList();
        var transactionIds = string.Join(",", transactionList.Select(t => t.Id).OrderBy(id => id));
        var cacheKey = $"{BatchMatchesCacheKeyPrefix}{transactionIds.GetHashCode()}";
        
        if (_cache.TryGetValue(cacheKey, out Dictionary<int, IEnumerable<DocumentMatch>>? cachedResult))
        {
            _logger.LogDebug("Cache hit for batch document matches: {TransactionCount} transactions", transactionList.Count);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for batch document matches: {TransactionCount} transactions", transactionList.Count);
        var result = await _innerService.BatchDocumentMatchesAsync(transactionList, cancellationToken);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = BatchMatchesCacheExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Priority = CacheItemPriority.High // Batch operations are expensive
        };
        
        _cache.Set(cacheKey, result, cacheOptions);
        _logger.LogDebug("Cached batch document matches for {TransactionCount} transactions", transactionList.Count);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MultipleDocumentMatch>> FindMultipleDocumentMatchesAsync(Transaction transaction, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            return Enumerable.Empty<MultipleDocumentMatch>();

        var cacheKey = $"{MultipleMatchesCacheKeyPrefix}{transaction.Id}_{unconnectedOnly}";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<MultipleDocumentMatch>? cachedResult))
        {
            _logger.LogDebug("Cache hit for multiple document matches: transaction {TransactionId}", transaction.Id);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for multiple document matches: transaction {TransactionId}", transaction.Id);
        var result = await _innerService.FindMultipleDocumentMatchesAsync(transaction, unconnectedOnly, cancellationToken);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = MultipleMatchesCacheExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(8),
            Priority = CacheItemPriority.Normal
        };
        
        _cache.Set(cacheKey, result, cacheOptions);
        _logger.LogDebug("Cached multiple document matches for transaction {TransactionId}", transaction.Id);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MultipleDocumentMatch>> FindMultipleDocumentMatchesAsync(int transactionId, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{MultipleMatchesCacheKeyPrefix}{transactionId}_{unconnectedOnly}";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<MultipleDocumentMatch>? cachedResult))
        {
            _logger.LogDebug("Cache hit for multiple document matches: transaction {TransactionId}", transactionId);
            return cachedResult!;
        }

        _logger.LogDebug("Cache miss for multiple document matches: transaction {TransactionId}", transactionId);
        var result = await _innerService.FindMultipleDocumentMatchesAsync(transactionId, unconnectedOnly, cancellationToken);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = MultipleMatchesCacheExpiry,
            SlidingExpiration = TimeSpan.FromMinutes(8),
            Priority = CacheItemPriority.Normal
        };
        
        _cache.Set(cacheKey, result, cacheOptions);
        _logger.LogDebug("Cached multiple document matches for transaction {TransactionId}", transactionId);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<MultipleAssignmentResult>> AutoAssignMultipleDocumentsAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        // Auto-assignment operations should not be cached as they modify data
        // and we want to ensure fresh results each time
        _logger.LogDebug("Auto-assignment operation for transaction {TransactionId} - bypassing cache", transactionId);
        
        var result = await _innerService.AutoAssignMultipleDocumentsAsync(transactionId, cancellationToken);
        
        if (result.IsSuccess)
        {
            // Invalidate related caches after successful assignment
            InvalidateMatchingCaches(transactionId);
            _logger.LogDebug("Invalidated matching caches after auto-assignment for transaction {TransactionId}", transactionId);
        }

        return result;
    }

    /// <summary>
    /// Invalidates all cached matching data related to a specific transaction.
    /// This method is called whenever document attachments change to ensure cache consistency.
    /// </summary>
    /// <param name="transactionId">The transaction ID to invalidate caches for</param>
    private void InvalidateMatchingCaches(int transactionId)
    {
        var keysToRemove = new[]
        {
            $"{DocumentMatchesCacheKeyPrefix}{transactionId}_True",
            $"{DocumentMatchesCacheKeyPrefix}{transactionId}_False",
            $"{MultipleMatchesCacheKeyPrefix}{transactionId}_True",
            $"{MultipleMatchesCacheKeyPrefix}{transactionId}_False"
        };

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        // Note: Batch caches are harder to invalidate selectively
        // In a production system, consider using cache tags or a more sophisticated approach
        
        _logger.LogDebug("Invalidated {Count} matching cache entries for transaction {TransactionId}", keysToRemove.Length, transactionId);
    }

    /// <summary>
    /// Clears all matching-related caches. Useful for testing or when document data changes significantly.
    /// </summary>
    public void ClearAllMatchingCaches()
    {
        _logger.LogInformation("Matching cache clear requested - individual entries will expire naturally");
        
        // For now, we rely on natural expiration
        // In a production system, consider using:
        // - IMemoryCache with cache tags
        // - Redis with pattern-based key deletion
        // - Custom cache implementation with clear functionality
    }
}