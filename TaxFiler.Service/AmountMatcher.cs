using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Interface for amount-based matching between transactions and documents.
/// </summary>
public interface IAmountMatcher
{
    /// <summary>
    /// Calculates the amount similarity score between a transaction and document.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="document">Document to match against</param>
    /// <param name="config">Amount matching configuration</param>
    /// <returns>Score between 0.0 and 1.0 indicating amount similarity</returns>
    double CalculateAmountScore(Transaction transaction, Document document, AmountMatchingConfig config);
}

/// <summary>
/// Implements amount-based matching logic for transaction-document matching.
/// Compares transaction amounts with document amounts using configurable tolerance ranges.
/// </summary>
public class AmountMatcher : IAmountMatcher
{
    /// <summary>
    /// Calculates the amount similarity score between a transaction and document.
    /// Uses tolerance ranges to determine scoring levels: exact, high, medium, or low match.
    /// </summary>
    /// <param name="transaction">Transaction containing GrossAmount for comparison</param>
    /// <param name="document">Document containing Total, SubTotal, TaxAmount, and Skonto fields</param>
    /// <param name="config">Configuration defining tolerance ranges for different score levels</param>
    /// <returns>Score between 0.0 and 1.0, where 1.0 indicates exact match within tolerance</returns>
    public double CalculateAmountScore(Transaction transaction, Document document, AmountMatchingConfig config)
    {
        if (transaction == null || document == null || config == null)
            return 0.0;

        var transactionAmount = transaction.GrossAmount;
        var documentAmount = GetBestDocumentAmount(document);

        if (documentAmount == null || documentAmount == 0)
            return 0.0;

        // Calculate percentage difference
        var difference = Math.Abs(transactionAmount - documentAmount.Value);
        var percentageDifference = (double)(difference / Math.Max(Math.Abs(transactionAmount), Math.Abs(documentAmount.Value)));

        // Determine score based on tolerance ranges
        if (percentageDifference <= config.ExactMatchTolerance)
            return 1.0; // Exact match within tolerance

        if (percentageDifference <= config.HighMatchTolerance)
            return 0.8; // High confidence match

        if (percentageDifference <= config.MediumMatchTolerance)
            return 0.5; // Medium confidence match

        // Low confidence - use inverse relationship for gradual scoring
        // Beyond medium tolerance, score decreases gradually to 0
        var maxToleranceForScoring = config.MediumMatchTolerance * 3; // Allow some scoring up to 3x medium tolerance
        if (percentageDifference <= maxToleranceForScoring)
        {
            var remainingTolerance = maxToleranceForScoring - config.MediumMatchTolerance;
            var excessDifference = percentageDifference - config.MediumMatchTolerance;
            return 0.2 * (1.0 - (excessDifference / remainingTolerance)); // Score from 0.2 down to 0
        }

        return 0.0; // No match
    }

    /// <summary>
    /// Selects the most appropriate amount from document fields for comparison.
    /// Priority: Total > SubTotal > (SubTotal + TaxAmount) > TaxAmount
    /// Considers Skonto (early payment discount) when available.
    /// </summary>
    /// <param name="document">Document containing various amount fields</param>
    /// <returns>Best available amount for matching, or null if no valid amount found</returns>
    private static decimal? GetBestDocumentAmount(Document document)
    {
        // Priority 1: Use Total if available (most comprehensive amount)
        if (document.Total.HasValue && document.Total.Value > 0)
        {
            // If Skonto is available, consider it as potential discount
            if (document.Skonto.HasValue && document.Skonto.Value > 0)
            {
                // Return both original total and total minus skonto for consideration
                // For now, return the original total as primary comparison
                return document.Total.Value;
            }
            return document.Total.Value;
        }

        // Priority 2: Calculate total from SubTotal + TaxAmount if both available
        if (document.SubTotal.HasValue && document.SubTotal.Value > 0 && 
            document.TaxAmount.HasValue && document.TaxAmount.Value > 0)
        {
            var calculatedTotal = document.SubTotal.Value + document.TaxAmount.Value;
            
            // Apply Skonto if available
            if (document.Skonto.HasValue && document.Skonto.Value > 0)
            {
                return calculatedTotal; // Return full amount, Skonto consideration handled elsewhere
            }
            return calculatedTotal;
        }

        // Priority 3: Use SubTotal if available
        if (document.SubTotal.HasValue && document.SubTotal.Value > 0)
        {
            return document.SubTotal.Value;
        }

        // Priority 4: Use TaxAmount as last resort (least reliable for matching)
        if (document.TaxAmount.HasValue && document.TaxAmount.Value > 0)
        {
            return document.TaxAmount.Value;
        }

        // No valid amount found
        return null;
    }

    /// <summary>
    /// Gets alternative amount considering Skonto discount for additional matching attempts.
    /// This can be used for secondary matching when primary amount doesn't match well.
    /// </summary>
    /// <param name="document">Document to get alternative amount from</param>
    /// <returns>Amount with Skonto discount applied, or null if not applicable</returns>
    public static decimal? GetSkontoAdjustedAmount(Document document)
    {
        var baseAmount = GetBestDocumentAmount(document);
        
        if (baseAmount.HasValue && document.Skonto.HasValue && document.Skonto.Value > 0)
        {
            return baseAmount.Value - document.Skonto.Value;
        }

        return null;
    }
}