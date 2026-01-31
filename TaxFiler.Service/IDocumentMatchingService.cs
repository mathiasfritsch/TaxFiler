using FluentResults;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

/// <summary>
/// Service interface for matching transactions with supporting documents using multi-criteria scoring.
/// Enhanced to support multiple document matching and automatic assignment.
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
    /// Finds and ranks multiple document combinations that could match a transaction.
    /// This method identifies sets of documents that together could represent the transaction,
    /// such as multiple invoices paid with a single bank transfer.
    /// </summary>
    /// <param name="transaction">The transaction to find multiple document matches for</param>
    /// <param name="unconnectedOnly">If true, only consider documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of multiple document match combinations ordered by score (highest first)</returns>
    Task<IEnumerable<MultipleDocumentMatch>> FindMultipleDocumentMatchesAsync(Transaction transaction, bool unconnectedOnly = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds and ranks multiple document combinations that could match a transaction by ID.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to find multiple document matches for</param>
    /// <param name="unconnectedOnly">If true, only consider documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of multiple document match combinations ordered by score (highest first)</returns>
    Task<IEnumerable<MultipleDocumentMatch>> FindMultipleDocumentMatchesAsync(int transactionId, bool unconnectedOnly = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Automatically assigns multiple documents to a transaction based on matching criteria.
    /// Uses enhanced matching logic to identify and attach the best combination of documents.
    /// Includes validation to prevent amount overages and provides audit logging.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to auto-assign documents to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing assignment details, warnings, and attached documents</returns>
    Task<Result<MultipleAssignmentResult>> AutoAssignMultipleDocumentsAsync(int transactionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs batch matching for multiple transactions.
    /// </summary>
    /// <param name="transactions">Collection of transactions to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping transaction IDs to their document matches</returns>
    Task<Dictionary<int, IEnumerable<DocumentMatch>>> BatchDocumentMatchesAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
}