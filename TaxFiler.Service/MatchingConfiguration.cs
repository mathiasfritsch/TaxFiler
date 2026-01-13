using System.ComponentModel.DataAnnotations;

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
    
    /// <summary>
    /// Validates the configuration values to ensure they are within acceptable ranges.
    /// </summary>
    /// <returns>A list of validation errors, empty if configuration is valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        // Validate weights are non-negative
        if (AmountWeight < 0)
            errors.Add("AmountWeight must be non-negative");
        if (DateWeight < 0)
            errors.Add("DateWeight must be non-negative");
        if (VendorWeight < 0)
            errors.Add("VendorWeight must be non-negative");
        if (ReferenceWeight < 0)
            errors.Add("ReferenceWeight must be non-negative");
            
        // Validate weights sum to a reasonable range (0.8 to 1.2 to allow some flexibility)
        var totalWeight = AmountWeight + DateWeight + VendorWeight + ReferenceWeight;
        if (totalWeight < 0.8 || totalWeight > 1.2)
            errors.Add($"Total weight sum ({totalWeight:F2}) should be between 0.8 and 1.2");
            
        // Validate threshold values are within 0.0-1.0 range
        if (MinimumMatchScore < 0.0 || MinimumMatchScore > 1.0)
            errors.Add("MinimumMatchScore must be between 0.0 and 1.0");
        if (BonusThreshold < 0.0 || BonusThreshold > 1.0)
            errors.Add("BonusThreshold must be between 0.0 and 1.0");
            
        // Validate bonus multiplier is positive and reasonable
        if (BonusMultiplier <= 0)
            errors.Add("BonusMultiplier must be positive");
        if (BonusMultiplier > 2.0)
            errors.Add("BonusMultiplier should not exceed 2.0 to maintain score stability");
            
        // Validate sub-configurations
        errors.AddRange(AmountConfig.Validate());
        errors.AddRange(DateConfig.Validate());
        errors.AddRange(VendorConfig.Validate());
        
        return errors;
    }
    
    /// <summary>
    /// Validates the configuration and throws an exception if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
    public void ValidateAndThrow()
    {
        var errors = Validate();
        if (errors.Any())
        {
            throw new ArgumentException($"Invalid configuration: {string.Join("; ", errors)}");
        }
    }
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
    
    /// <summary>
    /// Validates the amount matching configuration.
    /// </summary>
    /// <returns>A list of validation errors, empty if configuration is valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        // Validate tolerance values are positive
        if (ExactMatchTolerance < 0)
            errors.Add("ExactMatchTolerance must be positive");
        if (HighMatchTolerance < 0)
            errors.Add("HighMatchTolerance must be positive");
        if (MediumMatchTolerance < 0)
            errors.Add("MediumMatchTolerance must be positive");
            
        // Validate tolerance hierarchy (exact <= high <= medium)
        if (ExactMatchTolerance > HighMatchTolerance)
            errors.Add("ExactMatchTolerance should not exceed HighMatchTolerance");
        if (HighMatchTolerance > MediumMatchTolerance)
            errors.Add("HighMatchTolerance should not exceed MediumMatchTolerance");
            
        // Validate reasonable upper bounds (tolerances shouldn't be too large)
        if (MediumMatchTolerance > 0.5)
            errors.Add("MediumMatchTolerance should not exceed 0.5 (50%)");
            
        return errors;
    }
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
    
    /// <summary>
    /// Validates the date matching configuration.
    /// </summary>
    /// <returns>A list of validation errors, empty if configuration is valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        // Validate day values are non-negative
        if (ExactMatchDays < 0)
            errors.Add("ExactMatchDays must be non-negative");
        if (HighMatchDays < 0)
            errors.Add("HighMatchDays must be non-negative");
        if (MediumMatchDays < 0)
            errors.Add("MediumMatchDays must be non-negative");
            
        // Validate day hierarchy (exact <= high <= medium)
        if (ExactMatchDays > HighMatchDays)
            errors.Add("ExactMatchDays should not exceed HighMatchDays");
        if (HighMatchDays > MediumMatchDays)
            errors.Add("HighMatchDays should not exceed MediumMatchDays");
            
        // Validate reasonable upper bounds
        if (MediumMatchDays > 365)
            errors.Add("MediumMatchDays should not exceed 365 days");
            
        return errors;
    }
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
    
    /// <summary>
    /// Validates the vendor matching configuration.
    /// </summary>
    /// <returns>A list of validation errors, empty if configuration is valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        // Validate threshold is within 0.0-1.0 range
        if (FuzzyMatchThreshold < 0.0 || FuzzyMatchThreshold > 1.0)
            errors.Add("FuzzyMatchThreshold must be between 0.0 and 1.0");
            
        return errors;
    }
}