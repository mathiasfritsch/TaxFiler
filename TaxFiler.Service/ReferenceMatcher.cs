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

    /// <summary>
    /// Calculates the reference similarity score between a transaction and multiple documents.
    /// </summary>
    /// <param name="transaction">Transaction to match</param>
    /// <param name="documents">Documents to match against</param>
    /// <returns>Score between 0.0 and 1.0 indicating reference similarity for multiple documents</returns>
    double CalculateReferenceScore(Transaction transaction, IEnumerable<Document> documents);

    /// <summary>
    /// Extracts voucher numbers from a transaction note.
    /// </summary>
    /// <param name="transactionNote">Transaction note to parse</param>
    /// <returns>Collection of extracted voucher numbers</returns>
    IEnumerable<string> ExtractVoucherNumbers(string transactionNote);
}

/// <summary>
/// Implements reference number-based matching logic for transaction-document matching.
/// Compares transaction references with document invoice numbers using case-insensitive comparison.
/// The matching is direction-independent and works consistently for both incoming and outgoing transactions.
/// </summary>
public class ReferenceMatcher : IReferenceMatcher
{
    /// <summary>
    /// Calculates the reference similarity score between a transaction and document.
    /// Uses hierarchical matching: exact match > substring match > reverse substring match.
    /// All comparisons are case-insensitive and handle null/empty values gracefully.
    /// Checks the TransactionNote field for invoice numbers.
    /// </summary>
    /// <param name="transaction">Transaction containing TransactionNote field</param>
    /// <param name="document">Document containing InvoiceNumber field</param>
    /// <returns>Score between 0.0 and 1.0, where 1.0 indicates exact reference match</returns>
    public double CalculateReferenceScore(Transaction transaction, Document document)
    {
        var transactionRef = transaction.TransactionNote;
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
    /// Calculates the reference similarity score between a transaction and multiple documents.
    /// Uses the highest individual score among all documents, with bonus for multiple matches.
    /// </summary>
    /// <param name="transaction">Transaction containing TransactionNote field</param>
    /// <param name="documents">Documents to match against</param>
    /// <returns>Score between 0.0 and 1.0, where higher scores indicate better matches</returns>
    public double CalculateReferenceScore(Transaction transaction, IEnumerable<Document> documents)
    {
        if (documents == null || !documents.Any())
            return 0.0;

        var documentList = documents.ToList();
        if (documentList.Count == 1)
            return CalculateReferenceScore(transaction, documentList.First());

        // Extract voucher numbers from transaction note
        var voucherNumbers = ExtractVoucherNumbers(transaction.TransactionNote).ToList();
        if (!voucherNumbers.Any())
        {
            // Fall back to single document scoring if no voucher numbers found
            return documentList.Max(doc => CalculateReferenceScore(transaction, doc));
        }

        // Calculate scores for each document
        var documentScores = new List<double>();
        var matchedVouchers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in documentList)
        {
            var score = CalculateReferenceScore(transaction, document);
            documentScores.Add(score);

            // Check if this document matches any voucher number
            if (!string.IsNullOrWhiteSpace(document.InvoiceNumber))
            {
                var normalizedInvoice = NormalizeReference(document.InvoiceNumber);
                foreach (var voucher in voucherNumbers)
                {
                    var normalizedVoucher = NormalizeReference(voucher);
                    if (string.Equals(normalizedInvoice, normalizedVoucher, StringComparison.OrdinalIgnoreCase) ||
                        StringSimilarity.ContainsIgnoreCase(normalizedInvoice, normalizedVoucher) ||
                        StringSimilarity.ContainsIgnoreCase(normalizedVoucher, normalizedInvoice))
                    {
                        matchedVouchers.Add(voucher);
                    }
                }
            }
        }

        // Base score is the maximum individual score
        var baseScore = documentScores.Max();

        // Apply bonus for multiple voucher matches
        if (matchedVouchers.Count > 1)
        {
            var matchRatio = (double)matchedVouchers.Count / voucherNumbers.Count;
            var bonus = Math.Min(matchRatio * 0.2, 0.3); // Up to 30% bonus for multiple matches
            baseScore = Math.Min(baseScore + bonus, 1.0);
        }

        return baseScore;
    }

    /// <summary>
    /// Extracts voucher numbers from a transaction note.
    /// Supports common German voucher number patterns and multiple references in a single note.
    /// </summary>
    /// <param name="transactionNote">Transaction note to parse</param>
    /// <returns>Collection of extracted voucher numbers</returns>
    public IEnumerable<string> ExtractVoucherNumbers(string transactionNote)
    {
        if (string.IsNullOrWhiteSpace(transactionNote))
            return Enumerable.Empty<string>();

        var voucherNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var note = transactionNote.Trim();

        // Strategy: Try patterns in order of specificity, stop when we find matches

        // Pattern 1: Explicit German invoice patterns with keywords
        var explicitPatterns = new[]
        {
            @"(?:Rechnung|Invoice)\s*[:\-\.]?\s*([A-Z0-9\-\/\.]{3,})",
            @"(?:Rechnungs?nr|Rechnungsnummer|Invoice\s*No|Inv\s*No)\s*[:\-\.]?\s*([A-Z0-9\-\/\.]{3,})",
            @"(?:Beleg|Voucher)\s*[:\-\.]?\s*([A-Z0-9\-\/\.]{3,})"
        };

        foreach (var pattern in explicitPatterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(note, pattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1 && IsValidReference(match.Groups[1].Value))
                {
                    voucherNumbers.Add(match.Groups[1].Value.Trim());
                }
            }
        }

        // Pattern 2: Multiple references with connectors (und, and, &, +, sowie)
        var connectorPattern = @"\b([A-Z0-9\-\/\.]{3,})\s*(?:und|and|&|\+|sowie)\s*([A-Z0-9\-\/\.]{3,})\b";
        var connectorMatches = System.Text.RegularExpressions.Regex.Matches(note, connectorPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        foreach (System.Text.RegularExpressions.Match match in connectorMatches)
        {
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (match.Groups[i].Success && IsValidReference(match.Groups[i].Value))
                {
                    voucherNumbers.Add(match.Groups[i].Value.Trim());
                }
            }
        }

        // Pattern 3: Comma or semicolon separated references
        var separatorPattern = @"\b([A-Z0-9\-\/\.]{3,})(?:\s*[,;]\s*([A-Z0-9\-\/\.]{3,}))+";
        var separatorMatches = System.Text.RegularExpressions.Regex.Matches(note, separatorPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        foreach (System.Text.RegularExpressions.Match match in separatorMatches)
        {
            // Extract all voucher numbers from the matched text
            var matchedText = match.Value;
            var individualVouchers = System.Text.RegularExpressions.Regex.Matches(matchedText, @"\b([A-Z0-9\-\/\.]{3,})\b", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            foreach (System.Text.RegularExpressions.Match voucherMatch in individualVouchers)
            {
                if (IsValidReference(voucherMatch.Groups[1].Value))
                {
                    voucherNumbers.Add(voucherMatch.Groups[1].Value.Trim());
                }
            }
        }

        // Pattern 4: Range patterns (e.g., "INV-001 bis INV-003")
        var rangePattern = @"\b([A-Z0-9\-\/\.]{3,})\s*(?:bis|to|through)\s*([A-Z0-9\-\/\.]{3,})\b";
        var rangeMatches = System.Text.RegularExpressions.Regex.Matches(note, rangePattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        foreach (System.Text.RegularExpressions.Match match in rangeMatches)
        {
            if (match.Groups.Count > 2 && 
                IsValidReference(match.Groups[1].Value) && 
                IsValidReference(match.Groups[2].Value))
            {
                voucherNumbers.Add(match.Groups[1].Value.Trim());
                voucherNumbers.Add(match.Groups[2].Value.Trim());
            }
        }

        // Pattern 5: Standalone voucher-like patterns in context (before fallback)
        var contextualPattern = @"(?:zusätzlich|additional|also|außerdem|further|moreover)\s+([A-Z0-9\-\/\.]{3,})\b";
        var contextualMatches = System.Text.RegularExpressions.Regex.Matches(note, contextualPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        foreach (System.Text.RegularExpressions.Match match in contextualMatches)
        {
            if (match.Groups.Count > 1 && IsValidReference(match.Groups[1].Value))
            {
                voucherNumbers.Add(match.Groups[1].Value.Trim());
            }
        }

        // Pattern 6: Standalone alphanumeric patterns (only if no explicit patterns found)
        if (!voucherNumbers.Any())
        {
            var standalonePattern = @"\b([A-Z]{2,}[0-9]{3,}|[0-9]{4,}[A-Z]{1,}|[A-Z0-9\-]{5,})\b";
            var matches = System.Text.RegularExpressions.Regex.Matches(note, standalonePattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (IsValidReference(match.Value))
                {
                    voucherNumbers.Add(match.Value.Trim());
                }
            }
        }

        return voucherNumbers.Where(IsValidReference).Distinct(StringComparer.OrdinalIgnoreCase);
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

        var trimmed = reference.Trim();
        
        // Must have minimum length
        if (trimmed.Length < 3)
            return false;

        // Must contain at least one alphanumeric character
        if (!trimmed.Any(char.IsLetterOrDigit))
            return false;

        // Must contain at least one digit (voucher numbers typically have numbers)
        if (!trimmed.Any(char.IsDigit))
            return false;

        // Exclude common generic references
        var genericRefs = new[] { "N/A", "NA", "NONE", "NULL", "UNKNOWN", "TBD", "PENDING" };
        if (genericRefs.Contains(trimmed.ToUpperInvariant()))
            return false;

        // Exclude fragments that are just parts of words
        if (trimmed.All(char.IsLetter) && trimmed.Length < 5)
            return false;

        // Exclude fragments that don't look like voucher numbers
        // Valid voucher numbers should have a mix of letters and numbers or be structured
        var hasLetters = trimmed.Any(char.IsLetter);
        var hasDigits = trimmed.Any(char.IsDigit);
        var hasStructure = trimmed.Contains('-') || trimmed.Contains('/') || trimmed.Contains('.');

        // Accept if it has both letters and digits, or if it has structure
        if ((hasLetters && hasDigits) || hasStructure)
            return true;

        // Accept if it's a long numeric sequence (like order numbers)
        if (!hasLetters && hasDigits && trimmed.Length >= 4)
            return true;

        return false;
    }
}