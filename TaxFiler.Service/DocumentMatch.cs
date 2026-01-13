using TaxFiler.DB.Model;

namespace TaxFiler.Service;

/// <summary>
/// Represents a document match result with confidence score and detailed breakdown.
/// </summary>
public class DocumentMatch
{
    /// <summary>
    /// The matched document.
    /// </summary>
    public Document Document { get; set; } = null!;
    
    /// <summary>
    /// Overall match confidence score (0.0 to 1.0).
    /// </summary>
    public double MatchScore { get; set; }
    
    /// <summary>
    /// Detailed breakdown of how the match score was calculated.
    /// </summary>
    public MatchScoreBreakdown ScoreBreakdown { get; set; } = null!;
}

/// <summary>
/// Detailed breakdown of match score components.
/// </summary>
public class MatchScoreBreakdown
{
    /// <summary>
    /// Score based on amount similarity (0.0 to 1.0).
    /// </summary>
    public double AmountScore { get; set; }
    
    /// <summary>
    /// Score based on date proximity (0.0 to 1.0).
    /// </summary>
    public double DateScore { get; set; }
    
    /// <summary>
    /// Score based on vendor/counterparty matching (0.0 to 1.0).
    /// </summary>
    public double VendorScore { get; set; }
    
    /// <summary>
    /// Score based on reference number matching (0.0 to 1.0).
    /// </summary>
    public double ReferenceScore { get; set; }
    
    /// <summary>
    /// Final composite score after applying weights and bonuses (0.0 to 1.0).
    /// </summary>
    public double CompositeScore { get; set; }
}