using NUnit.Framework;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class MatchingConfigurationTests
{
    [Test]
    public void Validate_ValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var config = new MatchingConfiguration();

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_NegativeWeights_ReturnsErrors()
    {
        // Arrange
        var config = new MatchingConfiguration
        {
            AmountWeight = -0.1,
            DateWeight = -0.2
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("AmountWeight must be non-negative"));
        Assert.That(errors, Contains.Item("DateWeight must be non-negative"));
    }

    [Test]
    public void Validate_WeightSumOutOfRange_ReturnsError()
    {
        // Arrange
        var config = new MatchingConfiguration
        {
            AmountWeight = 0.1,
            DateWeight = 0.1,
            VendorWeight = 0.1,
            ReferenceWeight = 0.1 // Total = 0.4, below 0.8
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Has.Some.Contains("Total weight sum"));
    }

    [Test]
    public void Validate_ThresholdOutOfRange_ReturnsErrors()
    {
        // Arrange
        var config = new MatchingConfiguration
        {
            MinimumMatchScore = 1.5,
            BonusThreshold = -0.1
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("MinimumMatchScore must be between 0.0 and 1.0"));
        Assert.That(errors, Contains.Item("BonusThreshold must be between 0.0 and 1.0"));
    }

    [Test]
    public void Validate_InvalidBonusMultiplier_ReturnsErrors()
    {
        // Arrange
        var config = new MatchingConfiguration
        {
            BonusMultiplier = 0 // Invalid: must be positive
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("BonusMultiplier must be positive"));
    }

    [Test]
    public void ValidateAndThrow_InvalidConfiguration_ThrowsException()
    {
        // Arrange
        var config = new MatchingConfiguration
        {
            AmountWeight = -1.0
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.ValidateAndThrow());
    }

    [Test]
    public void ValidateAndThrow_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new MatchingConfiguration();

        // Act & Assert
        Assert.DoesNotThrow(() => config.ValidateAndThrow());
    }
}

[TestFixture]
public class AmountMatchingConfigTests
{
    [Test]
    public void Validate_ValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var config = new AmountMatchingConfig();

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_NegativeTolerances_ReturnsErrors()
    {
        // Arrange
        var config = new AmountMatchingConfig
        {
            ExactMatchTolerance = -0.01,
            HighMatchTolerance = -0.05
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("ExactMatchTolerance must be positive"));
        Assert.That(errors, Contains.Item("HighMatchTolerance must be positive"));
    }

    [Test]
    public void Validate_InvalidToleranceHierarchy_ReturnsErrors()
    {
        // Arrange
        var config = new AmountMatchingConfig
        {
            ExactMatchTolerance = 0.10,
            HighMatchTolerance = 0.05,  // Should be >= ExactMatchTolerance
            MediumMatchTolerance = 0.03 // Should be >= HighMatchTolerance
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("ExactMatchTolerance should not exceed HighMatchTolerance"));
        Assert.That(errors, Contains.Item("HighMatchTolerance should not exceed MediumMatchTolerance"));
    }
}

[TestFixture]
public class DateMatchingConfigTests
{
    [Test]
    public void Validate_ValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var config = new DateMatchingConfig();

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_NegativeDays_ReturnsErrors()
    {
        // Arrange
        var config = new DateMatchingConfig
        {
            ExactMatchDays = -1,
            HighMatchDays = -7
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("ExactMatchDays must be non-negative"));
        Assert.That(errors, Contains.Item("HighMatchDays must be non-negative"));
    }

    [Test]
    public void Validate_InvalidDayHierarchy_ReturnsErrors()
    {
        // Arrange
        var config = new DateMatchingConfig
        {
            ExactMatchDays = 10,
            HighMatchDays = 5,  // Should be >= ExactMatchDays
            MediumMatchDays = 3 // Should be >= HighMatchDays
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("ExactMatchDays should not exceed HighMatchDays"));
        Assert.That(errors, Contains.Item("HighMatchDays should not exceed MediumMatchDays"));
    }
}

[TestFixture]
public class VendorMatchingConfigTests
{
    [Test]
    public void Validate_ValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var config = new VendorMatchingConfig();

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_ThresholdOutOfRange_ReturnsError()
    {
        // Arrange
        var config = new VendorMatchingConfig
        {
            FuzzyMatchThreshold = 1.5 // Invalid: must be between 0.0 and 1.0
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.That(errors, Contains.Item("FuzzyMatchThreshold must be between 0.0 and 1.0"));
    }
}