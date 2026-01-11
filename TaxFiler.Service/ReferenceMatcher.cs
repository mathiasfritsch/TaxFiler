using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Interface for reference number-based matching between transactions and documents.
/// </summary>
public interface IReferenceMatcher
{
    /// <summary>
    /// Calculates the reference similarity score between a transaction and document.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="document">Document to match against</param>
    /// <returns>Score between 0.0 and 1.0 indicating reference similarity</returns>
    double CalculateReferenceScore(Transaction transaction, Document document);
}

/// <summary>
/// Implements reference number-based matching logic for transaction-document matching.
/// Compares transaction references with document invoice numbers using case-insensitive comparison.
/// </summary>
public class ReferenceMatcher : IReferenceMatcher
{
    /// <summary>
    /// Calculates the reference similarity score between a transaction and document.
    /// Uses hierarchical matching: exact match > substring match > reverse substring match.
    /// All comparisons are case-insensitive and handle null/empty values gracefully.
    /// </summary>
    /// <param name="transaction">Transaction containing TransactionReference field</param>
    /// <param name="document">Document containing InvoiceNumber field</param>
    /// <returns>Score between 0.0 and 1.0, where 1.0 indicates exact reference match</returns>
    public double CalculateReferenceScore(Transaction transaction, Document document)
    {
        if (transaction == null || document == null)
            return 0.0;

        var transactionRef = transaction.TransactionReference;
        var documentRef = document.InvoiceNumber;

        // Handle null/empty reference fields gracefully
        if (string.IsNullOrWhiteSpace(transactionRef) || string.IsNullOrWhiteSpace(documentRef))
            return 0.0;

        // Normalize references for comparison (trim whitespace, handle case)
        var normalizedTransactionRef = NormalizeReference(transactionRef);
        var normalizedDocumentRef = NormalizeReference(documentRef);

        // Level 1: Exact match (highest confidence)
        if (string.Equals(normalizedTransactionRef, normalizedDocumentRef, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        // Level 2: Transaction reference contains document invoice number (high confidence)
        if (StringSimilarity.ContainsIgnoreCase(normalizedTransactionRef, normalizedDocumentRef))
            return 0.8;

        // Level 3: Document invoice number contains transaction reference (medium-high confidence)
        if (StringSimilarity.ContainsIgnoreCase(normalizedDocumentRef, normalizedTransactionRef))
            return 0.7;

        // Level 4: Numeric reference matching (medium confidence)
        // Extract and compare numeric parts of references
        var numericScore = CalculateNumericReferenceMatch(normalizedTransactionRef, normalizedDocumentRef);
        if (numericScore > 0)
            return Math.Min(numericScore, 0.6); // Cap at 0.6 for numeric matches

        // Level 5: Alphanumeric pattern matching (low-medium confidence)
        var patternScore = CalculatePatternMatch(normalizedTransactionRef, normalizedDocumentRef);
        if (patternScore > 0)
            return Math.Min(patternScore, 0.4); // Cap at 0.4 for pattern matches

        return 0.0; // No match
    }

    /// <summary>
    /// Normalizes a reference string for comparison by removing extra whitespace,
    /// standardizing case, and removing common prefixes/suffixes.
    /// </summary>
    /// <param name="reference">Reference string to normalize</param>
    /// <returns>Normalized reference string</returns>
    private static string NormalizeReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return string.Empty;

        // Trim and convert to uppercase for consistent comparison
        var normalized = reference.Trim().ToUpperInvariant();

        // Remove common prefixes that might interfere with matching
        var prefixesToRemove = new[] { "INV", "INVOICE", "REF", "REFERENCE", "NO", "NR", "NUM" };
        foreach (var prefix in prefixesToRemove)
        {
            if (normalized.StartsWith(prefix + " ") || normalized.StartsWith(prefix + ".") || 
                normalized.StartsWith(prefix + "-") || normalized.StartsWith(prefix + ":"))
            {
                normalized = normalized.Substring(prefix.Length).TrimStart(' ', '.', '-', ':');
                break;
            }
        }

        // Remove common suffixes
        var suffixesToRemove = new[] { " INV", " INVOICE", " REF", " REFERENCE" };
        foreach (var suffix in suffixesToRemove)
        {
            if (normalized.EndsWith(suffix))
            {
                normalized = normalized.Substring(0, normalized.Length - suffix.Length).TrimEnd();
                break;
            }
        }

        // Standardize separators
        normalized = normalized.Replace('/', '-').Replace('_', '-').Replace('.', '-');
        
        // Remove extra spaces and hyphens
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-");

        return normalized.Trim();
    }

    /// <summary>
    /// Calculates similarity score based on numeric parts of references.
    /// Useful for matching invoice numbers that share numeric components.
    /// </summary>
    /// <param name="ref1">First reference (normalized)</param>
    /// <param name="ref2">Second reference (normalized)</param>
    /// <returns>Score between 0.0 and 1.0 based on numeric similarity</returns>
    private static double CalculateNumericReferenceMatch(string ref1, string ref2)
    {
        // Extract numeric parts from both references
        var numbers1 = ExtractNumbers(ref1);
        var numbers2 = ExtractNumbers(ref2);

        if (numbers1.Count == 0 || numbers2.Count == 0)
            return 0.0;

        // Find the best matching number pair
        double bestScore = 0.0;
        foreach (var num1 in numbers1)
        {
            foreach (var num2 in numbers2)
            {
                if (num1 == num2)
                {
                    // Exact numeric match - score based on number significance
                    var score = CalculateNumericSignificance(num1, ref1, ref2);
                    bestScore = Math.Max(bestScore, score);
                }
                else if (IsNumericallySimilar(num1, num2))
                {
                    // Similar numbers (e.g., 12345 and 12346)
                    var score = CalculateNumericSignificance(num1, ref1, ref2) * 0.7;
                    bestScore = Math.Max(bestScore, score);
                }
            }
        }

        return bestScore;
    }

    /// <summary>
    /// Extracts all numeric sequences from a reference string.
    /// </summary>
    /// <param name="reference">Reference string to extract numbers from</param>
    /// <returns>List of numeric strings found in the reference</returns>
    private static List<string> ExtractNumbers(string reference)
    {
        var numbers = new List<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(reference, @"\d+");
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            // Only include numbers with at least 3 digits (more significant)
            if (match.Value.Length >= 3)
            {
                numbers.Add(match.Value);
            }
        }

        return numbers;
    }

    /// <summary>
    /// Calculates the significance of a numeric match within the context of the full references.
    /// </summary>
    /// <param name="number">The matching number</param>
    /// <param name="ref1">First reference containing the number</param>
    /// <param name="ref2">Second reference containing the number</param>
    /// <returns>Significance score between 0.0 and 1.0</returns>
    private static double CalculateNumericSignificance(string number, string ref1, string ref2)
    {
        // Longer numbers are more significant
        var lengthScore = Math.Min(number.Length / 10.0, 1.0); // Cap at 10 digits
        
        // Numbers that make up a larger portion of the reference are more significant
        var ref1Ratio = (double)number.Length / ref1.Length;
        var ref2Ratio = (double)number.Length / ref2.Length;
        var ratioScore = (ref1Ratio + ref2Ratio) / 2.0;
        
        // Combine scores with emphasis on length
        return (lengthScore * 0.7) + (ratioScore * 0.3);
    }

    /// <summary>
    /// Determines if two numeric strings are similar (differ by small amount).
    /// </summary>
    /// <param name="num1">First numeric string</param>
    /// <param name="num2">Second numeric string</param>
    /// <returns>True if numbers are similar, false otherwise</returns>
    private static bool IsNumericallySimilar(string num1, string num2)
    {
        if (num1.Length != num2.Length)
            return false;

        if (long.TryParse(num1, out var n1) && long.TryParse(num2, out var n2))
        {
            var difference = Math.Abs(n1 - n2);
            // Consider similar if difference is small relative to the number size
            var threshold = Math.Max(1, (long)(Math.Max(n1, n2) * 0.01)); // 1% threshold
            return difference <= threshold;
        }

        return false;
    }

    /// <summary>
    /// Calculates similarity based on alphanumeric patterns in references.
    /// Useful for references with similar structure but different content.
    /// </summary>
    /// <param name="ref1">First reference (normalized)</param>
    /// <param name="ref2">Second reference (normalized)</param>
    /// <returns>Score between 0.0 and 1.0 based on pattern similarity</returns>
    private static double CalculatePatternMatch(string ref1, string ref2)
    {
        // Extract patterns (sequences of letters and numbers)
        var pattern1 = ExtractPattern(ref1);
        var pattern2 = ExtractPattern(ref2);

        if (pattern1 == pattern2)
            return 0.3; // Same pattern structure

        // Calculate Levenshtein similarity on patterns
        var similarity = StringSimilarity.LevenshteinSimilarity(pattern1, pattern2);
        if (similarity >= 0.8)
            return similarity * 0.25; // Scale down pattern matches

        return 0.0;
    }

    /// <summary>
    /// Extracts the structural pattern from a reference (e.g., "ABC123-DEF456" -> "LLL###-LLL###").
    /// </summary>
    /// <param name="reference">Reference to extract pattern from</param>
    /// <returns>Pattern string using L for letters, # for digits, and preserving separators</returns>
    private static string ExtractPattern(string reference)
    {
        var pattern = new System.Text.StringBuilder();
        
        foreach (var c in reference)
        {
            if (char.IsLetter(c))
                pattern.Append('L');
            else if (char.IsDigit(c))
                pattern.Append('#');
            else if (char.IsPunctuation(c) || char.IsSymbol(c))
                pattern.Append(c);
            // Skip whitespace in patterns
        }

        return pattern.ToString();
    }

    /// <summary>
    /// Determines if a reference appears to be a valid invoice or transaction reference.
    /// Useful for filtering out generic or system-generated references.
    /// </summary>
    /// <param name="reference">Reference to validate</param>
    /// <returns>True if reference appears to be meaningful for matching</returns>
    public static bool IsValidReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return false;

        var normalized = NormalizeReference(reference);
        
        // Must have minimum length
        if (normalized.Length < 3)
            return false;

        // Must contain at least one alphanumeric character
        if (!normalized.Any(char.IsLetterOrDigit))
            return false;

        // Exclude common generic references (check both original and normalized)
        var genericRefs = new[] { "N/A", "NA", "NONE", "NULL", "UNKNOWN", "TBD", "PENDING" };
        if (genericRefs.Contains(reference.Trim().ToUpperInvariant()) || genericRefs.Contains(normalized))
            return false;

        return true;
    }
}