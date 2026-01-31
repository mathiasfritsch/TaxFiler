namespace TaxFiler.Model.Dto;

/// <summary>
/// Represents a match between a transaction and multiple documents.
/// Used when a single transaction could be matched with a combination of multiple documents,
/// such as a single payment covering multiple invoices.
/// </summary>
public class MultipleDocumentMatch
{
    /// <summary>
    /// Collection of documents that together match the transaction
    /// </summary>
    public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
    
    /// <summary>
    /// Overall match score for this combination of documents (0.0 to 1.0)
    /// Higher scores indicate better matches
    /// </summary>
    public double MatchScore { get; set; }
    
    /// <summary>
    /// Total amount of all documents in this match combination
    /// </summary>
    public decimal TotalDocumentAmount { get; set; }
    
    /// <summary>
    /// Number of documents in this match combination
    /// </summary>
    public int DocumentCount { get; set; }
    
    /// <summary>
    /// Detailed breakdown of how the match score was calculated
    /// </summary>
    public MultipleMatchScoreBreakdown ScoreBreakdown { get; set; } = new();
    
    /// <summary>
    /// Indicates whether this match combination has any validation warnings
    /// </summary>
    public bool HasWarnings { get; set; }
    
    /// <summary>
    /// List of warnings about this match combination
    /// Examples: amount mismatches, missing references, etc.
    /// </summary>
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    
    /// <summary>
    /// Confidence level of this match (High, Medium, Low)
    /// Based on the overall match score and validation results
    /// </summary>
    public MatchConfidenceLevel ConfidenceLevel { get; set; }
}

/// <summary>
/// Detailed breakdown of scoring for multiple document matches
/// </summary>
public class MultipleMatchScoreBreakdown
{
    /// <summary>
    /// Combined amount similarity score for all documents (0.0 to 1.0)
    /// </summary>
    public double AmountScore { get; set; }
    
    /// <summary>
    /// Combined date proximity score for all documents (0.0 to 1.0)
    /// </summary>
    public double DateScore { get; set; }
    
    /// <summary>
    /// Combined vendor similarity score for all documents (0.0 to 1.0)
    /// </summary>
    public double VendorScore { get; set; }
    
    /// <summary>
    /// Combined reference similarity score for all documents (0.0 to 1.0)
    /// Particularly important for multiple document matching with voucher numbers
    /// </summary>
    public double ReferenceScore { get; set; }
    
    /// <summary>
    /// Final composite score after applying weights and bonuses (0.0 to 1.0)
    /// </summary>
    public double CompositeScore { get; set; }
    
    /// <summary>
    /// Number of documents that had exact amount matches
    /// </summary>
    public int ExactAmountMatches { get; set; }
    
    /// <summary>
    /// Number of documents that had reference matches (voucher numbers)
    /// </summary>
    public int ReferenceMatches { get; set; }
    
    /// <summary>
    /// Bonus applied for multiple voucher number matches
    /// </summary>
    public double MultipleReferenceBonus { get; set; }
}

/// <summary>
/// Confidence level for document matches
/// </summary>
public enum MatchConfidenceLevel
{
    /// <summary>
    /// Low confidence match (score < 0.4)
    /// Manual review recommended
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium confidence match (score 0.4 - 0.7)
    /// Good candidate for automatic assignment with warnings
    /// </summary>
    Medium,
    
    /// <summary>
    /// High confidence match (score > 0.7)
    /// Suitable for automatic assignment
    /// </summary>
    High
}