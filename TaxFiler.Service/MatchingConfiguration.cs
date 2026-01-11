namespace TaxFiler.Service;

/// <summary>
/// Configuration for document matching algorithm weights, thresholds, and criteria.
/// </summary>
public class MatchingConfiguration
{
    /// <summary>
    /// Weight for amount matching in composite score calculation (default: 0.40).
    /// </summary>
    public double AmountWeight { get; set; } = 0.40;
    
    /// <summary>
    /// Weight for date matching in composite score calculation (default: 0.25).
    /// </summary>
    public double DateWeight { get; set; } = 0.25;
    
    /// <summary>
    /// Weight for vendor matching in composite score calculation (default: 0.25).
    /// </summary>
    public double VendorWeight { get; set; } = 0.25;
    
    /// <summary>
    /// Weight for reference matching in composite score calculation (default: 0.10).
    /// </summary>
    public double ReferenceWeight { get; set; } = 0.10;
    
    /// <summary>
    /// Minimum composite score required for a match to be returned (default: 0.3).
    /// </summary>
    public double MinimumMatchScore { get; set; } = 0.3;
    
    /// <summary>
    /// Threshold above which individual criterion scores receive a bonus (default: 0.9).
    /// </summary>
    public double BonusThreshold { get; set; } = 0.9;
    
    /// <summary>
    /// Multiplier applied to composite score when any criterion exceeds bonus threshold (default: 1.1).
    /// </summary>
    public double BonusMultiplier { get; set; } = 1.1;
    
    /// <summary>
    /// Configuration for amount matching behavior.
    /// </summary>
    public AmountMatchingConfig AmountConfig { get; set; } = new();
    
    /// <summary>
    /// Configuration for date matching behavior.
    /// </summary>
    public DateMatchingConfig DateConfig { get; set; } = new();
    
    /// <summary>
    /// Configuration for vendor matching behavior.
    /// </summary>
    public VendorMatchingConfig VendorConfig { get; set; } = new();
}

/// <summary>
/// Configuration for amount-based matching tolerances.
/// </summary>
public class AmountMatchingConfig
{
    /// <summary>
    /// Tolerance percentage for exact amount matches (default: 0.01 = 1%).
    /// </summary>
    public double ExactMatchTolerance { get; set; } = 0.01;
    
    /// <summary>
    /// Tolerance percentage for high-confidence amount matches (default: 0.05 = 5%).
    /// </summary>
    public double HighMatchTolerance { get; set; } = 0.05;
    
    /// <summary>
    /// Tolerance percentage for medium-confidence amount matches (default: 0.10 = 10%).
    /// </summary>
    public double MediumMatchTolerance { get; set; } = 0.10;
}

/// <summary>
/// Configuration for date-based matching tolerances.
/// </summary>
public class DateMatchingConfig
{
    /// <summary>
    /// Maximum days difference for exact date matches (default: 0).
    /// </summary>
    public int ExactMatchDays { get; set; } = 0;
    
    /// <summary>
    /// Maximum days difference for high-confidence date matches (default: 7).
    /// </summary>
    public int HighMatchDays { get; set; } = 7;
    
    /// <summary>
    /// Maximum days difference for medium-confidence date matches (default: 30).
    /// </summary>
    public int MediumMatchDays { get; set; } = 30;
}

/// <summary>
/// Configuration for vendor/counterparty matching behavior.
/// </summary>
public class VendorMatchingConfig
{
    /// <summary>
    /// Minimum similarity threshold for fuzzy string matching (default: 0.8 = 80%).
    /// </summary>
    public double FuzzyMatchThreshold { get; set; } = 0.8;
}