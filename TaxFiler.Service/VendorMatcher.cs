using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Interface for vendor/counterparty-based matching between transactions and documents.
/// </summary>
public interface IVendorMatcher
{
    /// <summary>
    /// Calculates the vendor similarity score between a transaction and document.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="document">Document to match against</param>
    /// <param name="config">Vendor matching configuration</param>
    /// <returns>Score between 0.0 and 1.0 indicating vendor similarity</returns>
    double CalculateVendorScore(Transaction transaction, Document document, VendorMatchingConfig config);
}

/// <summary>
/// Implements vendor/counterparty-based matching logic for transaction-document matching.
/// Uses hierarchical matching: exact match > substring match > fuzzy match.
/// </summary>
public class VendorMatcher : IVendorMatcher
{
    /// <summary>
    /// Calculates the vendor similarity score between a transaction and document.
    /// Uses multiple transaction fields (Counterparty, SenderReceiver) and hierarchical matching.
    /// </summary>
    /// <param name="transaction">Transaction containing Counterparty and SenderReceiver fields</param>
    /// <param name="document">Document containing VendorName field</param>
    /// <param name="config">Configuration defining fuzzy match threshold</param>
    /// <returns>Score between 0.0 and 1.0, where 1.0 indicates exact match</returns>
    public double CalculateVendorScore(Transaction transaction, Document document, VendorMatchingConfig config)
    {
        if (transaction == null || document == null || config == null)
            return 0.0;

        var documentVendor = document.VendorName;
        if (string.IsNullOrWhiteSpace(documentVendor))
            return 0.0;

        // Get the best score from all available transaction vendor fields
        var counterpartyScore = CalculateVendorFieldScore(transaction.Counterparty, documentVendor, config);
        var senderReceiverScore = CalculateVendorFieldScore(transaction.SenderReceiver, documentVendor, config);

        // Return the highest score from all vendor field comparisons
        return Math.Max(counterpartyScore, senderReceiverScore);
    }

    /// <summary>
    /// Calculates vendor similarity score between a single transaction field and document vendor.
    /// Implements the matching hierarchy: exact > substring > reverse substring > fuzzy.
    /// </summary>
    /// <param name="transactionVendor">Vendor name from transaction field</param>
    /// <param name="documentVendor">Vendor name from document</param>
    /// <param name="config">Configuration for fuzzy matching threshold</param>
    /// <returns>Score between 0.0 and 1.0 based on matching hierarchy</returns>
    private static double CalculateVendorFieldScore(string transactionVendor, string documentVendor, VendorMatchingConfig config)
    {
        if (string.IsNullOrWhiteSpace(transactionVendor) || string.IsNullOrWhiteSpace(documentVendor))
            return 0.0;

        // Normalize both strings for comparison
        var normalizedTransaction = StringSimilarity.NormalizeForMatching(transactionVendor);
        var normalizedDocument = StringSimilarity.NormalizeForMatching(documentVendor);

        // Level 1: Exact match (highest confidence)
        if (string.Equals(normalizedTransaction, normalizedDocument, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        // Level 2: Transaction counterparty contains document vendor (high confidence)
        if (StringSimilarity.ContainsIgnoreCase(normalizedTransaction, normalizedDocument))
            return 0.8;

        // Level 3: Document vendor contains transaction counterparty (medium-high confidence)
        if (StringSimilarity.ContainsIgnoreCase(normalizedDocument, normalizedTransaction))
            return 0.7;

        // Level 4: Fuzzy string matching (medium confidence)
        var similarity = StringSimilarity.LevenshteinSimilarity(normalizedTransaction, normalizedDocument);
        if (similarity >= config.FuzzyMatchThreshold)
        {
            // Scale fuzzy match score: threshold gets 0.6, perfect match gets 0.9
            // This ensures fuzzy matches score lower than substring matches
            var scaledScore = 0.6 + (similarity - config.FuzzyMatchThreshold) * 0.3 / (1.0 - config.FuzzyMatchThreshold);
            return Math.Min(scaledScore, 0.9); // Cap at 0.9 to keep below exact match
        }

        // Level 5: Partial word matching (low-medium confidence)
        var partialScore = CalculatePartialWordMatch(normalizedTransaction, normalizedDocument);
        if (partialScore > 0)
            return Math.Min(partialScore, 0.5); // Cap at 0.5 for partial matches

        return 0.0; // No match
    }

    /// <summary>
    /// Calculates score based on partial word matching between vendor names.
    /// Useful for cases where vendor names share common words but aren't substrings.
    /// </summary>
    /// <param name="vendor1">First vendor name (normalized)</param>
    /// <param name="vendor2">Second vendor name (normalized)</param>
    /// <returns>Score between 0.0 and 0.5 based on word overlap</returns>
    private static double CalculatePartialWordMatch(string vendor1, string vendor2)
    {
        if (string.IsNullOrWhiteSpace(vendor1) || string.IsNullOrWhiteSpace(vendor2))
            return 0.0;

        // Split into words and filter out very short words (likely articles, prepositions)
        var words1 = vendor1.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length >= 3)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var words2 = vendor2.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length >= 3)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (words1.Count == 0 || words2.Count == 0)
            return 0.0;

        // Calculate Jaccard similarity (intersection over union)
        var intersection = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
        var union = words1.Union(words2, StringComparer.OrdinalIgnoreCase).Count();

        if (union == 0)
            return 0.0;

        var jaccardSimilarity = (double)intersection / union;
        
        // Scale to appropriate range for partial matches (0.0 to 0.4)
        return jaccardSimilarity * 0.4;
    }

    /// <summary>
    /// Gets all vendor-related fields from a transaction for comprehensive matching.
    /// </summary>
    /// <param name="transaction">Transaction to extract vendor fields from</param>
    /// <returns>List of non-empty vendor field values</returns>
    public static List<string> GetTransactionVendorFields(Transaction transaction)
    {
        var vendorFields = new List<string>();

        if (!string.IsNullOrWhiteSpace(transaction.Counterparty))
            vendorFields.Add(transaction.Counterparty);

        if (!string.IsNullOrWhiteSpace(transaction.SenderReceiver))
            vendorFields.Add(transaction.SenderReceiver);

        return vendorFields;
    }

    /// <summary>
    /// Determines if two vendor names are likely referring to the same entity.
    /// Uses a lower threshold than normal matching for entity resolution.
    /// </summary>
    /// <param name="vendor1">First vendor name</param>
    /// <param name="vendor2">Second vendor name</param>
    /// <param name="threshold">Minimum similarity threshold (default: 0.7)</param>
    /// <returns>True if vendors are likely the same entity</returns>
    public static bool AreLikelySameVendor(string vendor1, string vendor2, double threshold = 0.7)
    {
        if (string.IsNullOrWhiteSpace(vendor1) || string.IsNullOrWhiteSpace(vendor2))
            return false;

        var config = new VendorMatchingConfig { FuzzyMatchThreshold = threshold };
        var score = CalculateVendorFieldScore(vendor1, vendor2, config);
        
        return score >= threshold;
    }
}