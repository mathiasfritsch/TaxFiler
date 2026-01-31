using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

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
    private readonly IDocumentAttachmentService _attachmentService;
    private readonly ILogger<DocumentMatchingService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentMatchingService.
    /// </summary>
    /// <param name="context">Database context for accessing documents and transactions</param>
    /// <param name="config">Configuration for matching weights and thresholds</param>
    /// <param name="amountMatcher">Service for amount-based matching</param>
    /// <param name="dateMatcher">Service for date-based matching</param>
    /// <param name="vendorMatcher">Service for vendor-based matching</param>
    /// <param name="referenceMatcher">Service for reference-based matching</param>
    /// <param name="attachmentService">Service for managing document attachments</param>
    /// <param name="logger">Logger for audit trail and debugging</param>
    public DocumentMatchingService(
        TaxFilerContext context,
        MatchingConfiguration config,
        IAmountMatcher amountMatcher,
        IDateMatcher dateMatcher,
        IVendorMatcher vendorMatcher,
        IReferenceMatcher referenceMatcher,
        IDocumentAttachmentService attachmentService,
        ILogger<DocumentMatchingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        // Validate configuration on initialization
        _config.ValidateAndThrow();
        
        _amountMatcher = amountMatcher ?? throw new ArgumentNullException(nameof(amountMatcher));
        _dateMatcher = dateMatcher ?? throw new ArgumentNullException(nameof(dateMatcher));
        _vendorMatcher = vendorMatcher ?? throw new ArgumentNullException(nameof(vendorMatcher));
        _referenceMatcher = referenceMatcher ?? throw new ArgumentNullException(nameof(referenceMatcher));
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Finds and ranks matching documents for a given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to find matches for</param>
    /// <param name="unconnectedOnly">If true, only return documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of document matches ordered by score (highest first)</returns>
    public async Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(Transaction transaction, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            return Enumerable.Empty<DocumentMatch>();

        // Optimized query: Get documents and attachment info in a single query
        var query = _context.Documents.AsNoTracking();
        
        if (unconnectedOnly)
        {
            // Use a more efficient subquery approach
            query = query.Where(d => !_context.DocumentAttachments.Any(da => da.DocumentId == d.Id));
        }
        
        var documents = await query.ToListAsync(cancellationToken);

        // Get list of document IDs that have attachments for calculating Unconnected field
        // Cache this for the duration of the method to avoid repeated queries
        var documentsWithTransactions = await _context.DocumentAttachments
            .Select(da => (int?)da.DocumentId)
            .ToArrayAsync(cancellationToken);

        // Calculate matches for all documents
        var matches = new List<DocumentMatch>();
        
        foreach (var document in documents)
        {
            var match = CalculateDocumentMatch(transaction, document, documentsWithTransactions);
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
    /// <param name="unconnectedOnly">If true, only return documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of document matches ordered by score (highest first)</returns>
    public async Task<IEnumerable<DocumentMatch>> DocumentMatchesAsync(int transactionId, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null)
            return Enumerable.Empty<DocumentMatch>();

        return await DocumentMatchesAsync(transaction, unconnectedOnly, cancellationToken);
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

        // Optimized: Get all documents and attachments once for efficiency
        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => !_context.DocumentAttachments.Any(da => da.DocumentId == d.Id))
            .ToListAsync(cancellationToken);

        // Get list of document IDs that have attachments for calculating Unconnected field
        var documentsWithTransactions = await _context.DocumentAttachments
            .Select(da => (int?)da.DocumentId)
            .ToArrayAsync(cancellationToken);

        // Process each transaction
        foreach (var transaction in transactions)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var matches = new List<DocumentMatch>();
            
            foreach (var document in documents)
            {
                var match = CalculateDocumentMatch(transaction, document, documentsWithTransactions);
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
    /// Finds and ranks multiple document combinations that could match a transaction.
    /// This method identifies sets of documents that together could represent the transaction,
    /// such as multiple invoices paid with a single bank transfer.
    /// </summary>
    /// <param name="transaction">The transaction to find multiple document matches for</param>
    /// <param name="unconnectedOnly">If true, only consider documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of multiple document match combinations ordered by score (highest first)</returns>
    public async Task<IEnumerable<MultipleDocumentMatch>> FindMultipleDocumentMatchesAsync(Transaction transaction, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            return Enumerable.Empty<MultipleDocumentMatch>();

        _logger.LogInformation("Finding multiple document matches for transaction {TransactionId}", transaction.Id);

        // Optimized query: Get available documents with a single query
        var query = _context.Documents.AsNoTracking();
        
        if (unconnectedOnly)
        {
            // Use more efficient subquery approach
            query = query.Where(d => !_context.DocumentAttachments.Any(da => da.DocumentId == d.Id));
        }
        
        var documents = await query.ToListAsync(cancellationToken);

        if (documents.Count < 2)
        {
            _logger.LogDebug("Not enough documents ({Count}) for multiple document matching", documents.Count);
            return Enumerable.Empty<MultipleDocumentMatch>();
        }

        // Extract voucher numbers from transaction note for reference-based matching
        var voucherNumbers = _referenceMatcher.ExtractVoucherNumbers(transaction.TransactionNote).ToList();
        
        var multipleMatches = new List<MultipleDocumentMatch>();

        // Strategy 1: Reference-based matching (multiple voucher numbers)
        if (voucherNumbers.Count > 1)
        {
            var referenceMatches = await FindReferenceBasedMultipleMatches(transaction, documents, voucherNumbers, cancellationToken);
            multipleMatches.AddRange(referenceMatches);
        }

        // Strategy 2: Amount-based matching (documents that sum to transaction amount)
        var amountMatches = await FindAmountBasedMultipleMatches(transaction, documents, cancellationToken);
        multipleMatches.AddRange(amountMatches);

        // Strategy 3: Hybrid matching (combination of reference and amount matching)
        var hybridMatches = await FindHybridMultipleMatches(transaction, documents, voucherNumbers, cancellationToken);
        multipleMatches.AddRange(hybridMatches);

        // Remove duplicates and rank by score
        var uniqueMatches = DeduplicateMultipleMatches(multipleMatches);
        var rankedMatches = uniqueMatches
            .Where(m => m.MatchScore >= _config.MinimumMatchScore)
            .OrderByDescending(m => m.MatchScore)
            .Take(10) // Limit to top 10 combinations to avoid overwhelming results
            .ToList();

        _logger.LogInformation("Found {Count} multiple document match combinations for transaction {TransactionId}", 
            rankedMatches.Count, transaction.Id);

        return rankedMatches;
    }

    /// <summary>
    /// Finds and ranks multiple document combinations that could match a transaction by ID.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to find multiple document matches for</param>
    /// <param name="unconnectedOnly">If true, only consider documents not already connected to transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of multiple document match combinations ordered by score (highest first)</returns>
    public async Task<IEnumerable<MultipleDocumentMatch>> FindMultipleDocumentMatchesAsync(int transactionId, bool unconnectedOnly = true, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found for multiple document matching", transactionId);
            return Enumerable.Empty<MultipleDocumentMatch>();
        }

        return await FindMultipleDocumentMatchesAsync(transaction, unconnectedOnly, cancellationToken);
    }

    /// <summary>
    /// Automatically assigns multiple documents to a transaction based on matching criteria.
    /// Uses enhanced matching logic to identify and attach the best combination of documents.
    /// Includes validation to prevent amount overages and provides audit logging.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to auto-assign documents to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing assignment details, warnings, and attached documents</returns>
    public async Task<Result<MultipleAssignmentResult>> AutoAssignMultipleDocumentsAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting automatic multiple document assignment for transaction {TransactionId}", transactionId);

        try
        {
            // Get the transaction
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

            if (transaction == null)
            {
                return Result.Fail($"Transaction {transactionId} not found");
            }

            // Check if transaction already has attachments
            var existingAttachments = await _context.DocumentAttachments
                .Where(da => da.TransactionId == transactionId)
                .CountAsync(cancellationToken);

            if (existingAttachments > 0)
            {
                _logger.LogInformation("Transaction {TransactionId} already has {Count} attachments, skipping auto-assignment", 
                    transactionId, existingAttachments);
                return Result.Fail($"Transaction {transactionId} already has {existingAttachments} document(s) attached");
            }

            // Find multiple document matches
            var multipleMatches = await FindMultipleDocumentMatchesAsync(transaction, unconnectedOnly: true, cancellationToken);
            var bestMatch = multipleMatches.FirstOrDefault();

            if (bestMatch == null || bestMatch.MatchScore < _config.AutoAssignmentThreshold)
            {
                _logger.LogInformation("No suitable multiple document matches found for transaction {TransactionId} (best score: {Score})", 
                    transactionId, bestMatch?.MatchScore ?? 0);
                
                return Result.Ok(new MultipleAssignmentResult
                {
                    TransactionId = transactionId,
                    DocumentsAttached = 0,
                    TotalAmount = 0,
                    HasWarnings = true,
                    Warnings = new[] { "No suitable document combinations found for automatic assignment" },
                    AttachedDocuments = Enumerable.Empty<DocumentDto>()
                });
            }

            // Validate amounts before assignment
            var documentEntities = await _context.Documents
                .Where(d => bestMatch.Documents.Select(dto => dto.Id).Contains(d.Id))
                .ToListAsync(cancellationToken);

            var amountValidation = _amountMatcher.ValidateMultipleAmounts(transaction.GrossAmount, documentEntities);
            
            var warnings = new List<string>();
            if (amountValidation.HasWarnings)
            {
                warnings.AddRange(amountValidation.Warnings);
            }

            // Proceed with assignment even if there are warnings (but log them)
            var attachedDocuments = new List<DocumentDto>();
            var attachmentTasks = new List<Task<Result>>();

            foreach (var documentDto in bestMatch.Documents)
            {
                var attachTask = _attachmentService.AttachDocumentAsync(
                    transactionId, 
                    documentDto.Id, 
                    isAutomatic: true, 
                    attachedBy: "AutoAssignMultipleDocuments");
                attachmentTasks.Add(attachTask);
            }

            // Execute all attachments
            var attachmentResults = await Task.WhenAll(attachmentTasks);
            
            // Check for attachment failures
            var failedAttachments = attachmentResults.Where(r => r.IsFailed).ToList();
            if (failedAttachments.Any())
            {
                var errorMessages = failedAttachments.SelectMany(r => r.Errors.Select(e => e.Message));
                warnings.AddRange(errorMessages);
                _logger.LogWarning("Some document attachments failed for transaction {TransactionId}: {Errors}", 
                    transactionId, string.Join(", ", errorMessages));
            }

            // Get successfully attached documents
            var successfulAttachments = attachmentResults.Count(r => r.IsSuccess);
            if (successfulAttachments > 0)
            {
                attachedDocuments.AddRange(bestMatch.Documents.Take(successfulAttachments));
            }

            var result = new MultipleAssignmentResult
            {
                TransactionId = transactionId,
                DocumentsAttached = successfulAttachments,
                TotalAmount = bestMatch.TotalDocumentAmount,
                HasWarnings = warnings.Any(),
                Warnings = warnings,
                AttachedDocuments = attachedDocuments
            };

            _logger.LogInformation("Auto-assigned {Count} documents to transaction {TransactionId} with total amount {Amount}", 
                successfulAttachments, transactionId, bestMatch.TotalDocumentAmount);

            // Log audit trail
            _logger.LogInformation("Multiple document auto-assignment completed for transaction {TransactionId}: " +
                "Documents={DocumentIds}, TotalAmount={TotalAmount}, MatchScore={MatchScore}, Warnings={WarningCount}",
                transactionId, 
                string.Join(",", attachedDocuments.Select(d => d.Id)),
                bestMatch.TotalDocumentAmount,
                bestMatch.MatchScore,
                warnings.Count);

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic multiple document assignment for transaction {TransactionId}", transactionId);
            return Result.Fail($"Error during automatic assignment: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates the match score and breakdown for a transaction-document pair.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="document">Document to match against</param>
    /// <param name="documentsWithTransactions">Array of document IDs that have transactions</param>
    /// <returns>DocumentMatch with calculated scores</returns>
    private DocumentMatch CalculateDocumentMatch(Transaction transaction, Document document, int?[] documentsWithTransactions)
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

        // Convert Document entity to DocumentDto with Unconnected field
        var documentDto = document.ToDto(documentsWithTransactions);

        return new DocumentMatch
        {
            Document = documentDto,
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

    /// <summary>
    /// Finds document combinations based on reference matching (multiple voucher numbers).
    /// </summary>
    private async Task<List<MultipleDocumentMatch>> FindReferenceBasedMultipleMatches(
        Transaction transaction, 
        List<Document> documents, 
        List<string> voucherNumbers, 
        CancellationToken cancellationToken)
    {
        var matches = new List<MultipleDocumentMatch>();

        // Find documents that match the voucher numbers
        var matchingDocuments = new List<Document>();
        var matchedVouchers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in documents)
        {
            if (string.IsNullOrWhiteSpace(document.InvoiceNumber))
                continue;

            var normalizedInvoice = document.InvoiceNumber.Trim().ToUpperInvariant();
            
            foreach (var voucher in voucherNumbers)
            {
                var normalizedVoucher = voucher.Trim().ToUpperInvariant();
                
                if (normalizedInvoice.Equals(normalizedVoucher, StringComparison.OrdinalIgnoreCase) ||
                    normalizedInvoice.Contains(normalizedVoucher) ||
                    normalizedVoucher.Contains(normalizedInvoice))
                {
                    matchingDocuments.Add(document);
                    matchedVouchers.Add(voucher);
                    break;
                }
            }
        }

        if (matchingDocuments.Count >= 2)
        {
            var documentsWithTransactions = await GetDocumentsWithTransactions(cancellationToken);
            var documentDtos = matchingDocuments.Select(d => d.ToDto(documentsWithTransactions)).ToList();
            
            var multipleMatch = CreateMultipleDocumentMatch(transaction, documentDtos, matchingDocuments);
            
            // Boost score for reference-based matches
            multipleMatch.MatchScore = Math.Min(multipleMatch.MatchScore * 1.2, 1.0);
            multipleMatch.ScoreBreakdown.MultipleReferenceBonus = 0.2;
            multipleMatch.ScoreBreakdown.ReferenceMatches = matchedVouchers.Count;
            
            matches.Add(multipleMatch);
        }

        return matches;
    }

    /// <summary>
    /// Finds document combinations based on amount matching (documents that sum to transaction amount).
    /// </summary>
    private async Task<List<MultipleDocumentMatch>> FindAmountBasedMultipleMatches(
        Transaction transaction, 
        List<Document> documents, 
        CancellationToken cancellationToken)
    {
        var matches = new List<MultipleDocumentMatch>();
        var transactionAmount = Math.Abs(transaction.GrossAmount);
        
        // Try different combinations of documents (2-5 documents per combination)
        for (int combinationSize = 2; combinationSize <= Math.Min(5, documents.Count); combinationSize++)
        {
            var combinations = GetDocumentCombinations(documents, combinationSize);
            
            foreach (var combination in combinations.Take(50)) // Limit combinations to avoid performance issues
            {
                var totalAmount = combination.Sum(d => GetDocumentAmountForMatching(d));
                
                if (totalAmount == 0) continue;
                
                // Check if the combination amount is close to transaction amount
                var amountScore = _amountMatcher.CalculateMultipleAmountScore(transaction, combination, _config.AmountConfig);
                
                if (amountScore >= _config.MinimumMatchScore)
                {
                    var documentsWithTransactions = await GetDocumentsWithTransactions(cancellationToken);
                    var documentDtos = combination.Select(d => d.ToDto(documentsWithTransactions)).ToList();
                    
                    var multipleMatch = CreateMultipleDocumentMatch(transaction, documentDtos, combination);
                    matches.Add(multipleMatch);
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Finds document combinations using hybrid approach (reference + amount matching).
    /// </summary>
    private async Task<List<MultipleDocumentMatch>> FindHybridMultipleMatches(
        Transaction transaction, 
        List<Document> documents, 
        List<string> voucherNumbers, 
        CancellationToken cancellationToken)
    {
        var matches = new List<MultipleDocumentMatch>();

        if (!voucherNumbers.Any())
            return matches;

        // Find documents with partial reference matches and good amount matches
        var candidateDocuments = documents.Where(d => 
        {
            if (string.IsNullOrWhiteSpace(d.InvoiceNumber))
                return false;

            var singleScore = _referenceMatcher.CalculateReferenceScore(transaction, d);
            return singleScore >= 0.3; // Lower threshold for hybrid matching
        }).ToList();

        if (candidateDocuments.Count >= 2)
        {
            // Try combinations of candidate documents
            for (int combinationSize = 2; combinationSize <= Math.Min(4, candidateDocuments.Count); combinationSize++)
            {
                var combinations = GetDocumentCombinations(candidateDocuments, combinationSize);
                
                foreach (var combination in combinations.Take(20)) // Limit for performance
                {
                    var amountScore = _amountMatcher.CalculateMultipleAmountScore(transaction, combination, _config.AmountConfig);
                    var referenceScore = _referenceMatcher.CalculateReferenceScore(transaction, combination);
                    
                    // Require both decent amount and reference scores for hybrid matches
                    if (amountScore >= 0.4 && referenceScore >= 0.4)
                    {
                        var documentsWithTransactions = await GetDocumentsWithTransactions(cancellationToken);
                        var documentDtos = combination.Select(d => d.ToDto(documentsWithTransactions)).ToList();
                        
                        var multipleMatch = CreateMultipleDocumentMatch(transaction, documentDtos, combination);
                        
                        // Apply hybrid bonus
                        multipleMatch.MatchScore = Math.Min(multipleMatch.MatchScore * 1.1, 1.0);
                        
                        matches.Add(multipleMatch);
                    }
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Creates a MultipleDocumentMatch from transaction and document combination.
    /// </summary>
    private MultipleDocumentMatch CreateMultipleDocumentMatch(
        Transaction transaction, 
        List<DocumentDto> documentDtos, 
        IEnumerable<Document> documentEntities)
    {
        var documentList = documentEntities.ToList();
        
        // Calculate individual scores
        var amountScore = _amountMatcher.CalculateMultipleAmountScore(transaction, documentList, _config.AmountConfig);
        var dateScore = documentList.Average(d => _dateMatcher.CalculateDateScore(transaction, d, _config.DateConfig));
        var vendorScore = documentList.Average(d => _vendorMatcher.CalculateVendorScore(transaction, d, _config.VendorConfig));
        var referenceScore = _referenceMatcher.CalculateReferenceScore(transaction, documentList);

        // Calculate composite score
        var compositeScore = CalculateCompositeScore(amountScore, dateScore, vendorScore, referenceScore);
        var finalScore = ApplyBonusMultiplier(compositeScore, amountScore, dateScore, vendorScore, referenceScore);
        finalScore = Math.Max(0.0, Math.Min(1.0, finalScore));

        // Calculate total amount
        var totalAmount = documentList.Sum(d => GetDocumentAmountForMatching(d));

        // Validate amounts and generate warnings
        var validation = _amountMatcher.ValidateMultipleAmounts(transaction.GrossAmount, documentList);
        var warnings = validation.Warnings.ToList();

        // Determine confidence level
        var confidenceLevel = finalScore switch
        {
            >= 0.7 => MatchConfidenceLevel.High,
            >= 0.4 => MatchConfidenceLevel.Medium,
            _ => MatchConfidenceLevel.Low
        };

        return new MultipleDocumentMatch
        {
            Documents = documentDtos,
            MatchScore = finalScore,
            TotalDocumentAmount = totalAmount,
            DocumentCount = documentList.Count,
            ScoreBreakdown = new MultipleMatchScoreBreakdown
            {
                AmountScore = amountScore,
                DateScore = dateScore,
                VendorScore = vendorScore,
                ReferenceScore = referenceScore,
                CompositeScore = finalScore,
                ExactAmountMatches = documentList.Count(d => 
                    Math.Abs(GetDocumentAmountForMatching(d) - Math.Abs(transaction.GrossAmount)) < 0.01m),
                ReferenceMatches = documentList.Count(d => 
                    _referenceMatcher.CalculateReferenceScore(transaction, d) > 0.7)
            },
            HasWarnings = warnings.Any(),
            Warnings = warnings,
            ConfidenceLevel = confidenceLevel
        };
    }

    /// <summary>
    /// Gets document combinations of specified size.
    /// </summary>
    private static IEnumerable<List<Document>> GetDocumentCombinations(List<Document> documents, int combinationSize)
    {
        if (combinationSize == 1)
        {
            return documents.Select(d => new List<Document> { d });
        }

        if (combinationSize == documents.Count)
        {
            return new[] { documents };
        }

        if (combinationSize > documents.Count)
        {
            return Enumerable.Empty<List<Document>>();
        }

        return GetCombinationsRecursive(documents, combinationSize, 0);
    }

    /// <summary>
    /// Recursive helper for generating document combinations.
    /// </summary>
    private static IEnumerable<List<Document>> GetCombinationsRecursive(List<Document> documents, int combinationSize, int startIndex)
    {
        if (combinationSize == 1)
        {
            for (int i = startIndex; i < documents.Count; i++)
            {
                yield return new List<Document> { documents[i] };
            }
        }
        else
        {
            for (int i = startIndex; i <= documents.Count - combinationSize; i++)
            {
                var smallerCombinations = GetCombinationsRecursive(documents, combinationSize - 1, i + 1);
                foreach (var smallerCombination in smallerCombinations)
                {
                    var combination = new List<Document> { documents[i] };
                    combination.AddRange(smallerCombination);
                    yield return combination;
                }
            }
        }
    }

    /// <summary>
    /// Removes duplicate multiple matches based on document combinations.
    /// </summary>
    private static List<MultipleDocumentMatch> DeduplicateMultipleMatches(List<MultipleDocumentMatch> matches)
    {
        var uniqueMatches = new List<MultipleDocumentMatch>();
        var seenCombinations = new HashSet<string>();

        foreach (var match in matches.OrderByDescending(m => m.MatchScore))
        {
            var documentIds = match.Documents.Select(d => d.Id).OrderBy(id => id);
            var combinationKey = string.Join(",", documentIds);

            if (!seenCombinations.Contains(combinationKey))
            {
                seenCombinations.Add(combinationKey);
                uniqueMatches.Add(match);
            }
        }

        return uniqueMatches;
    }

    /// <summary>
    /// Gets the appropriate document amount for matching calculations.
    /// </summary>
    private static decimal GetDocumentAmountForMatching(Document document)
    {
        // Use the same logic as AmountMatcher.GetBestDocumentAmount
        if (document.Total.HasValue && document.Total.Value != 0)
            return document.Total.Value;

        if (document.SubTotal.HasValue && document.SubTotal.Value != 0 && 
            document.TaxAmount.HasValue && document.TaxAmount.Value != 0)
            return document.SubTotal.Value + document.TaxAmount.Value;

        if (document.SubTotal.HasValue && document.SubTotal.Value != 0)
            return document.SubTotal.Value;

        if (document.TaxAmount.HasValue && document.TaxAmount.Value != 0)
            return document.TaxAmount.Value;

        return 0;
    }

    /// <summary>
    /// Gets array of document IDs that have transactions (for DTO conversion).
    /// </summary>
    private async Task<int?[]> GetDocumentsWithTransactions(CancellationToken cancellationToken)
    {
        return await _context.DocumentAttachments
            .Select(da => (int?)da.DocumentId)
            .ToArrayAsync(cancellationToken);
    }
}