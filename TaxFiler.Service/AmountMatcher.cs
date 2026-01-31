using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Result of validating multiple document amounts against a transaction amount.
/// Provides validation status, warnings, and recommendations for amount discrepancies.
/// </summary>
public class MultipleAmountValidationResult
{
    /// <summary>
    /// Whether the validation passed (amounts are reasonable).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Total amount from all documents combined.
    /// </summary>
    public decimal TotalDocumentAmount { get; set; }

    /// <summary>
    /// The transaction amount being validated against.
    /// </summary>
    public decimal TransactionAmount { get; set; }

    /// <summary>
    /// Absolute difference between transaction and total document amounts.
    /// </summary>
    public decimal AmountDifference { get; set; }

    /// <summary>
    /// Percentage difference between transaction and total document amounts.
    /// </summary>
    public double PercentageDifference { get; set; }

    /// <summary>
    /// Number of documents that had valid amounts for calculation.
    /// </summary>
    public int ValidDocumentCount { get; set; }

    /// <summary>
    /// Number of documents that had Skonto applied.
    /// </summary>
    public int SkontoAppliedCount { get; set; }

    /// <summary>
    /// Warning messages about amount discrepancies or validation issues.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Recommendations for handling amount discrepancies.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Whether the total document amount significantly exceeds the transaction amount.
    /// </summary>
    public bool HasSignificantOverage { get; set; }

    /// <summary>
    /// Whether the total document amount is significantly less than the transaction amount.
    /// </summary>
    public bool HasSignificantUnderage { get; set; }

    /// <summary>
    /// Whether the validation result has any warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Any();
}

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

    /// <summary>
    /// Calculates the amount similarity score between a transaction and multiple documents.
    /// Sums the amounts from multiple documents and compares with transaction amount.
    /// Handles Skonto calculations across multiple documents.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="documents">Collection of documents to match against</param>
    /// <param name="config">Amount matching configuration</param>
    /// <returns>Score between 0.0 and 1.0 indicating amount similarity for the combined documents</returns>
    double CalculateMultipleAmountScore(Transaction transaction, IEnumerable<Document> documents, AmountMatchingConfig config);

    /// <summary>
    /// Validates that the total amount of multiple documents is reasonable compared to the transaction amount.
    /// Provides warnings for amount mismatches while allowing attachments.
    /// </summary>
    /// <param name="transactionAmount">The transaction amount to validate against</param>
    /// <param name="documents">Collection of documents to validate</param>
    /// <returns>Validation result with success/failure status and any warnings or recommendations</returns>
    MultipleAmountValidationResult ValidateMultipleAmounts(decimal transactionAmount, IEnumerable<Document> documents);
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
    /// The matching is direction-independent - works consistently for both incoming and outgoing transactions.
    /// Enhanced to handle Skonto (early payment discount) when present in documents.
    /// </summary>
    /// <param name="transaction">Transaction containing GrossAmount for comparison</param>
    /// <param name="document">Document containing Total, SubTotal, TaxAmount, and Skonto fields</param>
    /// <param name="config">Configuration defining tolerance ranges for different score levels</param>
    /// <returns>Score between 0.0 and 1.0, where 1.0 indicates exact match within tolerance</returns>
    public double CalculateAmountScore(Transaction transaction, Document document, AmountMatchingConfig config)
    {
        if (transaction == null || document == null || config == null)
            return 0.0;

        var transactionAmount = GetTransactionAmountForMatching(transaction);
        var documentAmount = GetBestDocumentAmount(document);

        if (documentAmount == null)
            return 0.0;

        // Enhanced Skonto handling with validation
        if (SkontoCalculator.HasValidSkonto(document.Skonto))
        {
            try
            {
                var originalAmount = documentAmount.Value;
                var discountedAmount = SkontoCalculator.CalculateDiscountedAmount(documentAmount.Value, document.Skonto);
                
                // Validate Skonto calculation results
                if (discountedAmount < 0)
                {
                    // If Skonto results in negative amount, use original amount
                    documentAmount = originalAmount;
                }
                else if (discountedAmount > originalAmount && originalAmount > 0)
                {
                    // If discounted amount is larger than original (invalid), use original
                    documentAmount = originalAmount;
                }
                else
                {
                    // Valid Skonto calculation
                    documentAmount = discountedAmount;
                }
            }
            catch (Exception)
            {
                // If Skonto calculation fails, continue without Skonto
            }
        }

        // Calculate percentage difference - since all amounts are positive, no need for Math.Abs()
        var difference = Math.Abs(transactionAmount - documentAmount.Value);
        var maxAmount = Math.Max(transactionAmount, documentAmount.Value);
        
        // Handle case where both amounts are zero
        if (maxAmount == 0)
            return transactionAmount == documentAmount.Value ? 1.0 : 0.0;
            
        var percentageDifference = (double)(difference / maxAmount);

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
    /// Calculates the amount similarity score between a transaction and multiple documents.
    /// Sums the amounts from multiple documents (applying Skonto where applicable) and compares with transaction amount.
    /// Uses the same tolerance ranges as single document matching for consistency.
    /// </summary>
    /// <param name="transaction">Transaction containing GrossAmount for comparison</param>
    /// <param name="documents">Collection of documents to sum amounts from</param>
    /// <param name="config">Configuration defining tolerance ranges for different score levels</param>
    /// <returns>Score between 0.0 and 1.0, where 1.0 indicates exact match within tolerance</returns>
    public double CalculateMultipleAmountScore(Transaction transaction, IEnumerable<Document> documents, AmountMatchingConfig config)
    {
        if (transaction == null || documents == null || config == null)
            return 0.0;

        var documentList = documents.ToList();
        if (!documentList.Any())
            return 0.0;

        var transactionAmount = GetTransactionAmountForMatching(transaction);
        var totalDocumentAmount = CalculateTotalDocumentAmount(documentList);

        if (totalDocumentAmount == 0)
            return 0.0;

        // Calculate percentage difference using the same logic as single document matching
        var difference = Math.Abs(transactionAmount - totalDocumentAmount);
        var maxAmount = Math.Max(transactionAmount, totalDocumentAmount);
        
        // Handle case where both amounts are zero
        if (maxAmount == 0)
            return transactionAmount == totalDocumentAmount ? 1.0 : 0.0;
            
        var percentageDifference = (double)(difference / maxAmount);

        // Use the same tolerance ranges as single document matching
        if (percentageDifference <= config.ExactMatchTolerance)
            return 1.0; // Exact match within tolerance

        if (percentageDifference <= config.HighMatchTolerance)
            return 0.8; // High confidence match

        if (percentageDifference <= config.MediumMatchTolerance)
            return 0.5; // Medium confidence match

        // Low confidence - use inverse relationship for gradual scoring
        var maxToleranceForScoring = config.MediumMatchTolerance * 3;
        if (percentageDifference <= maxToleranceForScoring)
        {
            var remainingTolerance = maxToleranceForScoring - config.MediumMatchTolerance;
            var excessDifference = percentageDifference - config.MediumMatchTolerance;
            return 0.2 * (1.0 - (excessDifference / remainingTolerance));
        }

        return 0.0; // No match
    }

    /// <summary>
    /// Validates that the total amount of multiple documents is reasonable compared to the transaction amount.
    /// Provides detailed analysis including warnings and recommendations for amount discrepancies.
    /// Allows attachments even with mismatches but provides clear feedback to users.
    /// </summary>
    /// <param name="transactionAmount">The transaction amount to validate against</param>
    /// <param name="documents">Collection of documents to validate</param>
    /// <returns>Detailed validation result with warnings and recommendations</returns>
    public MultipleAmountValidationResult ValidateMultipleAmounts(decimal transactionAmount, IEnumerable<Document> documents)
    {
        var result = new MultipleAmountValidationResult
        {
            TransactionAmount = Math.Abs(transactionAmount), // Use absolute value for comparison
            IsValid = true // Start optimistic, set to false if significant issues found
        };

        if (documents == null)
        {
            result.IsValid = false;
            result.Warnings.Add("No documents provided for validation");
            return result;
        }

        var documentList = documents.ToList();
        if (!documentList.Any())
        {
            result.IsValid = false;
            result.Warnings.Add("Document list is empty");
            return result;
        }

        // Calculate total amount and gather statistics
        var validDocuments = new List<(Document doc, decimal amount, bool skontoApplied)>();
        decimal totalAmount = 0;

        foreach (var document in documentList)
        {
            var documentAmount = GetBestDocumentAmount(document);
            if (!documentAmount.HasValue || documentAmount.Value == 0)
                continue;

            var finalAmount = documentAmount.Value;
            var skontoApplied = false;

            // Apply Skonto if valid
            if (SkontoCalculator.HasValidSkonto(document.Skonto))
            {
                try
                {
                    var discountedAmount = SkontoCalculator.CalculateDiscountedAmount(documentAmount.Value, document.Skonto);
                    
                    // Validate Skonto calculation - ensure it's reasonable
                    if (discountedAmount >= 0 && discountedAmount <= documentAmount.Value && document.Skonto <= 100.0m)
                    {
                        finalAmount = discountedAmount;
                        skontoApplied = true;
                    }
                    // If Skonto is invalid (e.g., > 100%), don't apply it
                }
                catch (Exception)
                {
                    // Continue with original amount if Skonto calculation fails
                }
            }

            validDocuments.Add((document, finalAmount, skontoApplied));
            totalAmount += finalAmount;
        }

        // Set result statistics
        result.TotalDocumentAmount = totalAmount;
        result.ValidDocumentCount = validDocuments.Count;
        result.SkontoAppliedCount = validDocuments.Count(v => v.skontoApplied);
        result.AmountDifference = Math.Abs(result.TransactionAmount - totalAmount);

        // Calculate percentage difference
        var maxAmount = Math.Max(result.TransactionAmount, totalAmount);
        result.PercentageDifference = maxAmount > 0 ? (double)(result.AmountDifference / maxAmount) : 0.0;

        // Analyze amount discrepancies and provide feedback
        AnalyzeAmountDiscrepancies(result);

        return result;
    }

    /// <summary>
    /// Calculates the total amount from multiple documents, applying Skonto where applicable.
    /// </summary>
    /// <param name="documents">Collection of documents to sum amounts from</param>
    /// <returns>Total amount from all valid documents</returns>
    private static decimal CalculateTotalDocumentAmount(IEnumerable<Document> documents)
    {
        decimal total = 0;

        foreach (var document in documents)
        {
            var documentAmount = GetBestDocumentAmount(document);
            if (!documentAmount.HasValue || documentAmount.Value == 0)
                continue;

            var finalAmount = documentAmount.Value;

            // Apply Skonto if valid, using the same logic as single document matching
            if (SkontoCalculator.HasValidSkonto(document.Skonto))
            {
                try
                {
                    var discountedAmount = SkontoCalculator.CalculateDiscountedAmount(documentAmount.Value, document.Skonto);
                    
                    // Validate Skonto calculation results - ensure it's reasonable
                    if (discountedAmount >= 0 && discountedAmount <= documentAmount.Value && document.Skonto <= 100.0m)
                    {
                        finalAmount = discountedAmount;
                    }
                    // If Skonto is invalid (e.g., > 100%), don't apply it
                }
                catch (Exception)
                {
                    // Continue with original amount if Skonto calculation fails
                }
            }

            total += finalAmount;
        }

        return total;
    }

    /// <summary>
    /// Analyzes amount discrepancies and populates warnings and recommendations.
    /// </summary>
    /// <param name="result">Validation result to analyze and update</param>
    private static void AnalyzeAmountDiscrepancies(MultipleAmountValidationResult result)
    {
        const double significantDifferenceThreshold = 0.10; // 10%
        const double minorDifferenceThreshold = 0.05; // 5%

        // Check for significant discrepancies
        if (result.PercentageDifference > significantDifferenceThreshold)
        {
            result.IsValid = false;

            if (result.TotalDocumentAmount > result.TransactionAmount)
            {
                result.HasSignificantOverage = true;
                result.Warnings.Add($"Document total (€{result.TotalDocumentAmount:F2}) significantly exceeds transaction amount (€{result.TransactionAmount:F2}) by {result.PercentageDifference:P1}");
                result.Recommendations.Add("Verify that all attached documents belong to this transaction");
                result.Recommendations.Add("Check if some documents should be attached to different transactions");
            }
            else
            {
                result.HasSignificantUnderage = true;
                result.Warnings.Add($"Document total (€{result.TotalDocumentAmount:F2}) is significantly less than transaction amount (€{result.TransactionAmount:F2}) by {result.PercentageDifference:P1}");
                result.Recommendations.Add("Additional documents may be missing for this transaction");
                result.Recommendations.Add("Verify that the transaction amount is correct");
            }
        }
        else if (result.PercentageDifference > minorDifferenceThreshold)
        {
            // Minor discrepancy - warn but don't mark as invalid
            if (result.TotalDocumentAmount > result.TransactionAmount)
            {
                result.Warnings.Add($"Document total slightly exceeds transaction amount by {result.PercentageDifference:P1}");
                result.Recommendations.Add("This may be due to rounding differences or fees");
            }
            else
            {
                result.Warnings.Add($"Document total is slightly less than transaction amount by {result.PercentageDifference:P1}");
                result.Recommendations.Add("This may be due to rounding differences or partial payments");
            }
        }

        // Provide information about Skonto applications
        if (result.SkontoAppliedCount > 0)
        {
            result.Recommendations.Add($"Skonto (early payment discount) was applied to {result.SkontoAppliedCount} document(s)");
        }

        // Check for edge cases
        if (result.ValidDocumentCount == 0)
        {
            result.IsValid = false;
            result.Warnings.Add("No documents have valid amounts for comparison");
            result.Recommendations.Add("Ensure documents have proper amount information");
        }
        else if (result.TotalDocumentAmount == 0)
        {
            result.IsValid = false;
            result.Warnings.Add("Total document amount is zero");
            result.Recommendations.Add("Verify document amount data is correctly populated");
        }
    }

    /// <summary>
    /// Gets the appropriate transaction amount for matching purposes.
    /// Since all amounts are positive, this ensures consistent matching regardless of transaction direction.
    /// </summary>
    /// <param name="transaction">Transaction to get amount from</param>
    /// <returns>Transaction amount for matching (always positive)</returns>
    private static decimal GetTransactionAmountForMatching(Transaction transaction)
    {
        // All amounts are positive, so we can use GrossAmount directly
        // This ensures direction independence - both incoming and outgoing transactions
        // are matched using their positive amount values
        return transaction.GrossAmount;
    }

    /// <summary>
    /// Selects the most appropriate amount from document fields for comparison.
    /// Priority: Total > SubTotal + TaxAmount > SubTotal > TaxAmount
    /// </summary>
    /// <param name="document">Document containing various amount fields</param>
    /// <returns>Best available amount for matching, or null if no valid amount found</returns>
    private static decimal? GetBestDocumentAmount(Document document)
    {
        // Priority 1: Use Total if available (most comprehensive amount)
        if (document.Total.HasValue && document.Total.Value != 0)
        {
            return document.Total.Value;
        }

        // Priority 2: Calculate total from SubTotal + TaxAmount if both available
        if (document.SubTotal.HasValue && document.SubTotal.Value != 0 && 
            document.TaxAmount.HasValue && document.TaxAmount.Value != 0)
        {
            return document.SubTotal.Value + document.TaxAmount.Value;
        }

        // Priority 3: Use SubTotal if available
        if (document.SubTotal.HasValue && document.SubTotal.Value != 0)
        {
            return document.SubTotal.Value;
        }

        // Priority 4: Use TaxAmount as last resort (least reliable for matching)
        if (document.TaxAmount.HasValue && document.TaxAmount.Value != 0)
        {
            return document.TaxAmount.Value;
        }

        // No valid amount found
        return null;
    }

    /// <summary>
    /// Gets alternative amount considering Skonto discount for additional matching attempts.
    /// This method is now deprecated as Skonto logic is integrated into CalculateAmountScore.
    /// Kept for backward compatibility.
    /// </summary>
    /// <param name="document">Document to get alternative amount from</param>
    /// <returns>Amount with Skonto discount applied, or null if not applicable</returns>
    [Obsolete("Skonto logic is now integrated into CalculateAmountScore. This method is kept for backward compatibility.")]
    public static decimal? GetSkontoAdjustedAmount(Document document)
    {
        var baseAmount = GetBestDocumentAmount(document);
        
        if (baseAmount.HasValue && SkontoCalculator.HasValidSkonto(document.Skonto))
        {
            return SkontoCalculator.CalculateDiscountedAmount(baseAmount.Value, document.Skonto);
        }

        return null;
    }
}