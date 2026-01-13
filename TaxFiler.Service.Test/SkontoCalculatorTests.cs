using NUnit.Framework;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class SkontoCalculatorTests
{
    [Test]
    public void CalculateDiscountedAmount_ValidSkonto_ReturnsCorrectAmount()
    {
        // Arrange
        var documentTotal = 1000m;
        var skontoPercentage = 2.0m; // 2%
        var expected = 980m; // 1000 - (1000 * 0.02)

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CalculateDiscountedAmount_NullSkonto_ReturnsOriginalAmount()
    {
        // Arrange
        var documentTotal = 1000m;
        decimal? skontoPercentage = null;

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(documentTotal));
    }

    [Test]
    public void CalculateDiscountedAmount_ZeroSkonto_ReturnsOriginalAmount()
    {
        // Arrange
        var documentTotal = 1000m;
        var skontoPercentage = 0m;

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(documentTotal));
    }

    [Test]
    public void CalculateDiscountedAmount_NegativeSkonto_ReturnsOriginalAmount()
    {
        // Arrange
        var documentTotal = 1000m;
        var skontoPercentage = -5m;

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(documentTotal));
    }

    [Test]
    public void CalculateDiscountedAmount_SkontoOver100Percent_CappedAt100Percent()
    {
        // Arrange
        var documentTotal = 1000m;
        var skontoPercentage = 150m; // 150% - should be capped at 100%

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(0m)); // 100% discount = 0
    }

    [Test]
    public void CalculateDiscountedAmount_ZeroDocumentTotal_ReturnsZero()
    {
        // Arrange
        var documentTotal = 0m;
        var skontoPercentage = 2.0m;

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void HasValidSkonto_ValidPercentage_ReturnsTrue()
    {
        // Arrange
        var skontoPercentage = 2.5m;

        // Act
        var result = SkontoCalculator.HasValidSkonto(skontoPercentage);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasValidSkonto_NullPercentage_ReturnsFalse()
    {
        // Arrange
        decimal? skontoPercentage = null;

        // Act
        var result = SkontoCalculator.HasValidSkonto(skontoPercentage);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasValidSkonto_ZeroPercentage_ReturnsFalse()
    {
        // Arrange
        var skontoPercentage = 0m;

        // Act
        var result = SkontoCalculator.HasValidSkonto(skontoPercentage);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasValidSkonto_NegativePercentage_ReturnsFalse()
    {
        // Arrange
        var skontoPercentage = -1m;

        // Act
        var result = SkontoCalculator.HasValidSkonto(skontoPercentage);

        // Assert
        Assert.That(result, Is.False);
    }

    // Additional edge case tests for boundary conditions and rounding scenarios
    
    [Test]
    public void CalculateDiscountedAmount_NegativeDocumentTotal_ReturnsOriginalAmount()
    {
        // Arrange
        var documentTotal = -100m;
        var skontoPercentage = 2.0m;

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(documentTotal));
    }

    [Test]
    public void CalculateDiscountedAmount_VerySmallAmount_HandlesRoundingCorrectly()
    {
        // Arrange
        var documentTotal = 0.01m; // 1 cent
        var skontoPercentage = 2.0m; // 2%
        var expected = 0.0098m; // 0.01 - (0.01 * 0.02)

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CalculateDiscountedAmount_VerySmallPercentage_HandlesRoundingCorrectly()
    {
        // Arrange
        var documentTotal = 1000m;
        var skontoPercentage = 0.01m; // 0.01%
        var expected = 999.9m; // 1000 - (1000 * 0.0001)

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CalculateDiscountedAmount_LargeAmount_HandlesCorrectly()
    {
        // Arrange
        var documentTotal = 999999999.99m; // Very large amount
        var skontoPercentage = 2.0m; // 2%
        var expected = 979999999.9902m; // Large amount - 2%

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CalculateDiscountedAmount_ExactlyOneHundredPercent_ReturnsZero()
    {
        // Arrange
        var documentTotal = 500m;
        var skontoPercentage = 100m; // Exactly 100%

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculateDiscountedAmount_CommonGermanSkontoRates_HandlesCorrectly()
    {
        // Test common German Skonto rates (2% and 3%)
        
        // 2% Skonto
        var result2Percent = SkontoCalculator.CalculateDiscountedAmount(1000m, 2.0m);
        Assert.That(result2Percent, Is.EqualTo(980m));
        
        // 3% Skonto
        var result3Percent = SkontoCalculator.CalculateDiscountedAmount(1000m, 3.0m);
        Assert.That(result3Percent, Is.EqualTo(970m));
        
        // 2.5% Skonto (less common but valid)
        var result2Point5Percent = SkontoCalculator.CalculateDiscountedAmount(1000m, 2.5m);
        Assert.That(result2Point5Percent, Is.EqualTo(975m));
    }

    [Test]
    public void CalculateDiscountedAmount_DecimalPrecisionEdgeCase_HandlesCorrectly()
    {
        // Arrange - amount that could cause rounding issues
        var documentTotal = 123.456789m;
        var skontoPercentage = 2.5m; // 2.5%
        // Calculate expected value: 123.456789 - (123.456789 * 0.025) = 123.456789 - 3.086419725 = 120.370369275
        var expected = 120.370369275m;

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void HasValidSkonto_VerySmallPositivePercentage_ReturnsTrue()
    {
        // Arrange
        var skontoPercentage = 0.001m; // Very small but positive

        // Act
        var result = SkontoCalculator.HasValidSkonto(skontoPercentage);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasValidSkonto_VeryLargePercentage_ReturnsTrue()
    {
        // Arrange - HasValidSkonto doesn't validate upper bounds
        var skontoPercentage = 999m; // Very large percentage

        // Act
        var result = SkontoCalculator.HasValidSkonto(skontoPercentage);

        // Assert
        Assert.That(result, Is.True);
    }

    // Additional tests for enhanced error handling integration
    
    [Test]
    public void CalculateDiscountedAmount_ExtremelyLargePercentage_CappedCorrectly()
    {
        // Arrange
        var documentTotal = 1000m;
        var skontoPercentage = decimal.MaxValue; // Extremely large percentage

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert - Should be capped at 100% discount (result = 0)
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void CalculateDiscountedAmount_MinimumDecimalValue_HandlesCorrectly()
    {
        // Arrange
        var documentTotal = decimal.MinValue; // Most negative decimal value
        var skontoPercentage = 2.0m;

        // Act
        var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);

        // Assert - Should return original amount since it's negative
        Assert.That(result, Is.EqualTo(documentTotal));
    }

    [Test]
    public void CalculateDiscountedAmount_MaximumDecimalValue_HandlesCorrectly()
    {
        // Arrange
        var documentTotal = decimal.MaxValue; // Largest possible decimal
        var skontoPercentage = 2.0m; // 2%

        // Act & Assert - Should not throw overflow exception
        Assert.DoesNotThrow(() => 
        {
            var result = SkontoCalculator.CalculateDiscountedAmount(documentTotal, skontoPercentage);
            // Result should be less than original amount
            Assert.That(result, Is.LessThan(documentTotal));
            Assert.That(result, Is.GreaterThan(0));
        });
    }
}