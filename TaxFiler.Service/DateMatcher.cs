using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Interface for date-based matching between transactions and documents.
/// </summary>
public interface IDateMatcher
{
    /// <summary>
    /// Calculates the date proximity score between a transaction and document.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="document">Document to match against</param>
    /// <param name="config">Date matching configuration</param>
    /// <returns>Score between 0.0 and 1.0 indicating date proximity</returns>
    double CalculateDateScore(Transaction transaction, Document document, DateMatchingConfig config);
}

/// <summary>
/// Implements date-based matching logic for transaction-document matching.
/// Compares transaction dates with document dates using configurable tolerance ranges.
/// The matching is direction-independent and works consistently for both incoming and outgoing transactions.
/// </summary>
public class DateMatcher : IDateMatcher
{
    /// <summary>
    /// Calculates the date proximity score between a transaction and document.
    /// Uses configurable day tolerances to determine scoring levels: exact, high, medium, or low match.
    /// </summary>
    /// <param name="transaction">Transaction containing TransactionDateTime for comparison</param>
    /// <param name="document">Document containing InvoiceDate and InvoiceDateFromFolder fields</param>
    /// <param name="config">Configuration defining day tolerances for different score levels</param>
    /// <returns>Score between 0.0 and 1.0, where 1.0 indicates exact date match</returns>
    public double CalculateDateScore(Transaction transaction, Document document, DateMatchingConfig config)
    {
        if (transaction == null || document == null || config == null)
            return 0.0;

        var transactionDate = DateOnly.FromDateTime(transaction.TransactionDateTime);
        var documentDate = GetBestDocumentDate(document);

        if (documentDate == null)
            return 0.0;

        // Calculate absolute difference in days
        var daysDifference = Math.Abs(transactionDate.DayNumber - documentDate.Value.DayNumber);

        // Determine score based on configured tolerances
        if (daysDifference <= config.ExactMatchDays)
            return 1.0; // Exact match

        if (daysDifference <= config.HighMatchDays)
            return 0.8; // High confidence match

        if (daysDifference <= config.MediumMatchDays)
            return 0.5; // Medium confidence match

        // Low confidence - gradual scoring for dates beyond medium tolerance
        // Allow some scoring up to 3x medium tolerance (e.g., 90 days if medium is 30)
        var maxToleranceForScoring = config.MediumMatchDays * 3;
        if (daysDifference <= maxToleranceForScoring)
        {
            var remainingTolerance = maxToleranceForScoring - config.MediumMatchDays;
            var excessDays = daysDifference - config.MediumMatchDays;
            return 0.2 * (1.0 - ((double)excessDays / remainingTolerance)); // Score from 0.2 down to 0
        }

        return 0.0; // No match for dates too far apart
    }

    /// <summary>
    /// Selects the best available date from document fields for comparison.
    /// Priority: InvoiceDate > InvoiceDateFromFolder
    /// Handles null/missing date fields gracefully.
    /// </summary>
    /// <param name="document">Document containing date fields</param>
    /// <returns>Best available date for matching, or null if no valid date found</returns>
    private static DateOnly? GetBestDocumentDate(Document document)
    {
        // Priority 1: Use InvoiceDate if available (most reliable)
        if (document.InvoiceDate.HasValue)
        {
            return document.InvoiceDate.Value;
        }

        // Priority 2: Use InvoiceDateFromFolder as fallback
        if (document.InvoiceDateFromFolder.HasValue)
        {
            return document.InvoiceDateFromFolder.Value;
        }

        // No valid date found
        return null;
    }

    /// <summary>
    /// Gets alternative date for secondary matching attempts.
    /// Returns the lower priority date field if primary date was used.
    /// </summary>
    /// <param name="document">Document to get alternative date from</param>
    /// <returns>Alternative date for matching, or null if not available</returns>
    public static DateOnly? GetAlternativeDocumentDate(Document document)
    {
        // If InvoiceDate was used as primary, return InvoiceDateFromFolder as alternative
        if (document.InvoiceDate.HasValue && document.InvoiceDateFromFolder.HasValue)
        {
            return document.InvoiceDateFromFolder.Value;
        }

        // No alternative date available
        return null;
    }

    /// <summary>
    /// Calculates the number of days between two dates.
    /// Useful for debugging and detailed scoring analysis.
    /// </summary>
    /// <param name="date1">First date</param>
    /// <param name="date2">Second date</param>
    /// <returns>Absolute number of days between the dates</returns>
    public static int CalculateDaysDifference(DateOnly date1, DateOnly date2)
    {
        return Math.Abs(date1.DayNumber - date2.DayNumber);
    }

    /// <summary>
    /// Determines if two dates are within the specified tolerance.
    /// </summary>
    /// <param name="date1">First date</param>
    /// <param name="date2">Second date</param>
    /// <param name="toleranceDays">Maximum allowed difference in days</param>
    /// <returns>True if dates are within tolerance, false otherwise</returns>
    public static bool AreWithinTolerance(DateOnly date1, DateOnly date2, int toleranceDays)
    {
        return CalculateDaysDifference(date1, date2) <= toleranceDays;
    }
}