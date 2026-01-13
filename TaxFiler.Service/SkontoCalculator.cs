namespace TaxFiler.Service;

/// <summary>
/// Static utility class for calculating Skonto (early payment discount) amounts.
/// Handles percentage-based discount calculations and validation for German tax documents.
/// </summary>
public static class SkontoCalculator
{
    /// <summary>
    /// Calculates the discounted amount after applying Skonto percentage.
    /// </summary>
    /// <param name="documentTotal">The original document total amount</param>
    /// <param name="skontoPercentage">The Skonto percentage (e.g., 2.0 for 2%)</param>
    /// <returns>The document total minus the percentage-based discount</returns>
    /// <remarks>
    /// If skontoPercentage is null, zero, or negative, returns the original documentTotal.
    /// Skonto percentages over 100% are capped at 100% to prevent negative amounts.
    /// </remarks>
    public static decimal CalculateDiscountedAmount(decimal documentTotal, decimal? skontoPercentage)
    {
        // Handle null, zero, or negative Skonto percentages
        if (!skontoPercentage.HasValue || skontoPercentage.Value <= 0)
            return documentTotal;
        
        // Handle edge case where document total is zero or negative
        if (documentTotal <= 0)
            return documentTotal;
        
        // Cap Skonto percentage at 100% to prevent negative amounts
        var effectivePercentage = Math.Min(skontoPercentage.Value, 100m);
        
        // Calculate discount amount: documentTotal * (percentage / 100)
        var discountAmount = documentTotal * (effectivePercentage / 100m);
        
        // Return discounted amount: documentTotal - discountAmount
        var discountedAmount = documentTotal - discountAmount;
        
        // Ensure result is not negative (additional safety check)
        return Math.Max(discountedAmount, 0m);
    }
    
    /// <summary>
    /// Determines whether a document has a valid Skonto percentage for discount calculation.
    /// </summary>
    /// <param name="skontoPercentage">The Skonto percentage to validate</param>
    /// <returns>True if the Skonto percentage is valid (not null and greater than zero), false otherwise</returns>
    /// <remarks>
    /// A valid Skonto percentage must be:
    /// - Not null
    /// - Greater than zero
    /// Note: This method does not validate upper bounds - use CalculateDiscountedAmount for safe calculation.
    /// </remarks>
    public static bool HasValidSkonto(decimal? skontoPercentage)
    {
        return skontoPercentage.HasValue && skontoPercentage.Value > 0;
    }
}