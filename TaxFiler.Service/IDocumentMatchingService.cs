using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Service interface for matching transactions with supporting documents using multi-criteria scoring.
/// </summary>
public interface IDocumentMatchingService
{
    /// <summary>
    /// Finds and ranks matching documents for a given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to find matches for</param>
    /// <param name="unconnectedOnly">If true, only return documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of document matches ordered by score (highest first)</returns>
    Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(Transaction transaction, bool unconnectedOnly = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds and ranks matching documents for a transaction by ID.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to find matches for</param>
    /// <param name="unconnectedOnly">If true, only return documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of document matches ordered by score (highest first)</returns>
    Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(int transactionId, bool unconnectedOnly = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs batch matching for multiple transactions.
    /// </summary>
    /// <param name="transactions">Collection of transactions to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping transaction IDs to their document matches</returns>
    Task<Dictionary<int, IEnumerable<DocumentMatch>>> BatchDocumentMatchesAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
}