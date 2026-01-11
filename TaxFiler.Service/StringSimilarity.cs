using System.Globalization;
using System.Text;

namespace TaxFiler.Service;

/// <summary>
/// Utility class for string similarity calculations and text normalization.
/// Provides methods for fuzzy string matching used in document-transaction matching.
/// </summary>
public static class StringSimilarity
{
    /// <summary>
    /// Calculates the Levenshtein similarity between two strings.
    /// Returns a value between 0.0 (completely different) and 1.0 (identical).
    /// </summary>
    /// <param name="source">First string to compare</param>
    /// <param name="target">Second string to compare</param>
    /// <returns>Similarity score between 0.0 and 1.0</returns>
    public static double LevenshteinSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0;
        
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;
        
        // Normalize strings for comparison
        var normalizedSource = NormalizeForMatching(source);
        var normalizedTarget = NormalizeForMatching(target);
        
        if (normalizedSource == normalizedTarget)
            return 1.0;
        
        var distance = CalculateLevenshteinDistance(normalizedSource, normalizedTarget);
        var maxLength = Math.Max(normalizedSource.Length, normalizedTarget.Length);
        
        // Convert distance to similarity (1.0 - normalized distance)
        return 1.0 - (double)distance / maxLength;
    }
    
    /// <summary>
    /// Checks if the source string contains the target string, ignoring case.
    /// </summary>
    /// <param name="source">String to search in</param>
    /// <param name="target">String to search for</param>
    /// <returns>True if source contains target (case-insensitive), false otherwise</returns>
    public static bool ContainsIgnoreCase(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return false;
        
        return source.Contains(target, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Normalizes a string for matching by removing extra whitespace, converting to lowercase,
    /// and removing diacritics. This helps improve matching accuracy for German text.
    /// </summary>
    /// <param name="input">String to normalize</param>
    /// <returns>Normalized string suitable for comparison</returns>
    public static string NormalizeForMatching(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        // Convert to lowercase and trim
        var normalized = input.Trim().ToLowerInvariant();
        
        // Remove diacritics (important for German text like ä, ö, ü, ß)
        normalized = RemoveDiacritics(normalized);
        
        // Replace multiple whitespace with single space
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
        
        // Remove common punctuation that might interfere with matching
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[.,;:!?()[\]{}""'-]", "");
        
        return normalized.Trim();
    }
    
    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// This is the minimum number of single-character edits required to transform one string into another.
    /// </summary>
    /// <param name="source">First string</param>
    /// <param name="target">Second string</param>
    /// <returns>Levenshtein distance as integer</returns>
    private static int CalculateLevenshteinDistance(string source, string target)
    {
        var sourceLength = source.Length;
        var targetLength = target.Length;
        
        // Create a matrix to store distances
        var matrix = new int[sourceLength + 1, targetLength + 1];
        
        // Initialize first row and column
        for (var i = 0; i <= sourceLength; i++)
            matrix[i, 0] = i;
        
        for (var j = 0; j <= targetLength; j++)
            matrix[0, j] = j;
        
        // Fill the matrix
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                
                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,     // deletion
                        matrix[i, j - 1] + 1),   // insertion
                    matrix[i - 1, j - 1] + cost  // substitution
                );
            }
        }
        
        return matrix[sourceLength, targetLength];
    }
    
    /// <summary>
    /// Removes diacritics (accents) from characters, converting them to their base form.
    /// This is particularly useful for German text processing.
    /// </summary>
    /// <param name="text">Text to process</param>
    /// <returns>Text with diacritics removed</returns>
    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();
        
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}