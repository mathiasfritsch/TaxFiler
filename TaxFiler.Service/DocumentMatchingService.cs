using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Service for matching transactions with supporting documents using multi-criteria scoring.
/// Implements intelligent matching based on amount, date, vendor, and reference number similarity.
/// The matching algorithm is direction-independent and works consistently for both incoming and outgoing transactions.
/// </summary>
public class DocumentMatchingService : IDocumentMatchingService
{
    private readonly TaxFilerContext _context;
    private readonly MatchingConfiguration _config;
    private readonly IAmountMatcher _amountMatcher;
    private readonly IDateMatcher _dateMatcher;
    private readonly IVendorMatcher _vendorMatcher;
    private readonly IReferenceMatcher _referenceMatcher;

    /// <summary>
    /// Initializes a new instance of the DocumentMatchingService.
    /// </summary>
    /// <param name="context">Database context for accessing documents and transactions</param>
    /// <param name="config">Configuration for matching weights and thresholds</param>
    /// <param name="amountMatcher">Service for amount-based matching</param>
    /// <param name="dateMatcher">Service for date-based matching</param>
    /// <param name="vendorMatcher">Service for vendor-based matching</param>
    /// <param name="referenceMatcher">Service for reference-based matching</param>
    public DocumentMatchingService(
        TaxFilerContext context,
        MatchingConfiguration config,
        IAmountMatcher amountMatcher,
        IDateMatcher dateMatcher,
        IVendorMatcher vendorMatcher,
        IReferenceMatcher referenceMatcher)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        // Validate configuration on initialization
        _config.ValidateAndThrow();
        
        _amountMatcher = amountMatcher ?? throw new ArgumentNullException(nameof(amountMatcher));
        _dateMatcher = dateMatcher ?? throw new ArgumentNullException(nameof(dateMatcher));
        _vendorMatcher = vendorMatcher ?? throw new ArgumentNullException(nameof(vendorMatcher));
        _referenceMatcher = referenceMatcher ?? throw new ArgumentNullException(nameof(referenceMatcher));
    }

    /// <summary>
    /// Finds and ranks matching documents for a given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to find matches for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of document matches ordered by score (highest first)</returns>
    public async Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            return Enumerable.Empty<DocumentMatch>();

        // Get all documents from database
        var documents = await _context.Documents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Calculate matches for all documents
        var matches = new List<DocumentMatch>();
        
        foreach (var document in documents)
        {
            var match = CalculateDocumentMatch(transaction, document);
            if (match.MatchScore >= _config.MinimumMatchScore)
            {
                matches.Add(match);
            }
        }

        // Return matches ordered by score (highest first)
        return matches.OrderByDescending(m => m.MatchScore);
    }

    /// <summary>
    /// Finds and ranks matching documents for a transaction by ID.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to find matches for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of document matches ordered by score (highest first)</returns>
    public async Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null)
            return Enumerable.Empty<DocumentMatch>();

        return await DocumentMatchesAsync(transaction, cancellationToken);
    }

    /// <summary>
    /// Performs batch matching for multiple transactions.
    /// </summary>
    /// <param name="transactions">Collection of transactions to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping transaction IDs to their document matches</returns>
    public async Task<Dictionary<int, IEnumerable<DocumentMatch>>> BatchDocumentMatchesAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        if (transactions == null)
            return new Dictionary<int, IEnumerable<DocumentMatch>>();

        var result = new Dictionary<int, IEnumerable<DocumentMatch>>();

        // Get all documents once for efficiency
        var documents = await _context.Documents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Process each transaction
        foreach (var transaction in transactions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var matches = new List<DocumentMatch>();
            
            foreach (var document in documents)
            {
                var match = CalculateDocumentMatch(transaction, document);
                if (match.MatchScore >= _config.MinimumMatchScore)
                {
                    matches.Add(match);
                }
            }

            // Order matches by score and add to result
            result[transaction.Id] = matches.OrderByDescending(m => m.MatchScore);
        }

        return result;
    }

    /// <summary>
    /// Calculates the match score and breakdown for a transaction-document pair.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="document">Document to match against</param>
    /// <returns>DocumentMatch with calculated scores</returns>
    private DocumentMatch CalculateDocumentMatch(Transaction transaction, Document document)
    {
        // Calculate individual criterion scores
        var amountScore = _amountMatcher.CalculateAmountScore(transaction, document, _config.AmountConfig);
        var dateScore = _dateMatcher.CalculateDateScore(transaction, document, _config.DateConfig);
        var vendorScore = _vendorMatcher.CalculateVendorScore(transaction, document, _config.VendorConfig);
        var referenceScore = _referenceMatcher.CalculateReferenceScore(transaction, document);

        // Calculate weighted composite score
        var compositeScore = CalculateCompositeScore(amountScore, dateScore, vendorScore, referenceScore);

        // Apply bonus multiplier if any individual score exceeds threshold
        var finalScore = ApplyBonusMultiplier(compositeScore, amountScore, dateScore, vendorScore, referenceScore);

        // Ensure score is within valid range
        finalScore = Math.Max(0.0, Math.Min(1.0, finalScore));

        return new DocumentMatch
        {
            Document = document,
            MatchScore = finalScore,
            ScoreBreakdown = new MatchScoreBreakdown
            {
                AmountScore = amountScore,
                DateScore = dateScore,
                VendorScore = vendorScore,
                ReferenceScore = referenceScore,
                CompositeScore = finalScore
            }
        };
    }

    /// <summary>
    /// Calculates the weighted composite score from individual criterion scores.
    /// </summary>
    /// <param name="amountScore">Amount similarity score</param>
    /// <param name="dateScore">Date proximity score</param>
    /// <param name="vendorScore">Vendor similarity score</param>
    /// <param name="referenceScore">Reference similarity score</param>
    /// <returns>Weighted composite score</returns>
    private double CalculateCompositeScore(double amountScore, double dateScore, double vendorScore, double referenceScore)
    {
        return (amountScore * _config.AmountWeight) +
               (dateScore * _config.DateWeight) +
               (vendorScore * _config.VendorWeight) +
               (referenceScore * _config.ReferenceWeight);
    }

    /// <summary>
    /// Applies bonus multiplier to composite score if any individual criterion exceeds the bonus threshold.
    /// </summary>
    /// <param name="compositeScore">Base composite score</param>
    /// <param name="amountScore">Amount similarity score</param>
    /// <param name="dateScore">Date proximity score</param>
    /// <param name="vendorScore">Vendor similarity score</param>
    /// <param name="referenceScore">Reference similarity score</param>
    /// <returns>Final score with bonus applied if applicable</returns>
    private double ApplyBonusMultiplier(double compositeScore, double amountScore, double dateScore, double vendorScore, double referenceScore)
    {
        // Check if any individual criterion exceeds the bonus threshold
        var hasHighScore = amountScore >= _config.BonusThreshold ||
                          dateScore >= _config.BonusThreshold ||
                          vendorScore >= _config.BonusThreshold ||
                          referenceScore >= _config.BonusThreshold;

        if (hasHighScore)
        {
            return compositeScore * _config.BonusMultiplier;
        }

        return compositeScore;
    }
}