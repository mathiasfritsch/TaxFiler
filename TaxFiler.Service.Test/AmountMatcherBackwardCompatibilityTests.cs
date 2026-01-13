using TaxFiler.DB.Model;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

/// <summary>
/// Tests to ensure backward compatibility of AmountMatcher with documents that have no Skonto terms.
/// These tests verify that the enhanced Skonto-aware implementation behaves identically to the original
/// implementation when processing documents without Skonto.
/// </summary>
[TestFixture]
public class AmountMatcherBackwardCompatibilityTests
{
    private AmountMatcher _matcher;
    private AmountMatchingConfig _config;

    [SetUp]
    public void SetUp()
    {
        _matcher = new AmountMatcher();
        _config = new AmountMatchingConfig
        {
            ExactMatchTolerance = 0.01,  // 1%
            HighMatchTolerance = 0.05,   // 5%
            MediumMatchTolerance = 0.10  // 10%
        };
    }

    [Test]
    public void CalculateAmountScore_DocumentWithNullSkonto_BehavesAsOriginal()
    {
        // Arrange - Document with null Skonto (most common case)
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document 
        { 
            Total = 100.00m,
            Skonto = null // Explicitly null Skonto
        };

        // Act
        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Assert - Should behave exactly as original implementation
        Assert.That(score, Is.EqualTo(1.0), "Documents with null Skonto should match exactly like original implementation");
    }

    [Test]
    public void CalculateAmountScore_DocumentWithZeroSkonto_BehavesAsOriginal()
    {
        // Arrange - Document with zero Skonto
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document 
        { 
            Total = 100.00m,
            Skonto = 0.00m // Zero Skonto
        };

        // Act
        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Assert - Should behave exactly as original implementation
        Assert.That(score, Is.EqualTo(1.0), "Documents with zero Skonto should match exactly like original implementation");
    }

    [Test]
    public void CalculateAmountScore_DocumentWithNegativeSkonto_BehavesAsOriginal()
    {
        // Arrange - Document with negative Skonto (invalid, should be ignored)
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document 
        { 
            Total = 100.00m,
            Skonto = -2.00m // Negative Skonto (invalid)
        };

        // Act
        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Assert - Should behave exactly as original implementation (ignore invalid Skonto)
        Assert.That(score, Is.EqualTo(1.0), "Documents with negative Skonto should be ignored and match like original implementation");
    }

    [Test]
    public void CalculateAmountScore_MultipleDocumentsWithoutSkonto_ConsistentScoring()
    {
        // Arrange - Multiple documents without Skonto terms
        var transaction = new Transaction { GrossAmount = 100.00m };
        
        var documents = new[]
        {
            new Document { Total = 100.00m, Skonto = null },
            new Document { Total = 103.00m, Skonto = null }, // 3% difference
            new Document { Total = 108.00m, Skonto = null }, // 8% difference
            new Document { Total = 120.00m, Skonto = null }  // 20% difference
        };

        // Act & Assert - Each document should score according to original logic
        var score1 = _matcher.CalculateAmountScore(transaction, documents[0], _config);
        Assert.That(score1, Is.EqualTo(1.0), "Exact match should score 1.0");

        var score2 = _matcher.CalculateAmountScore(transaction, documents[1], _config);
        Assert.That(score2, Is.EqualTo(0.8), "3% difference should score 0.8 (high match)");

        var score3 = _matcher.CalculateAmountScore(transaction, documents[2], _config);
        Assert.That(score3, Is.EqualTo(0.5), "8% difference should score 0.5 (medium match)");

        var score4 = _matcher.CalculateAmountScore(transaction, documents[3], _config);
        Assert.That(score4, Is.GreaterThan(0.0).And.LessThan(0.2), "20% difference should score low but > 0");
    }

    [Test]
    public void CalculateAmountScore_DocumentAmountPriority_UnchangedBehavior()
    {
        // Arrange - Test that document amount selection priority remains unchanged
        var transaction = new Transaction { GrossAmount = 119.00m };
        
        // Document with all amount fields but no Skonto
        var document = new Document 
        { 
            Total = 119.00m,      // Should be used (highest priority)
            SubTotal = 100.00m,
            TaxAmount = 19.00m,
            Skonto = null
        };

        // Act
        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Assert - Should use Total field (original behavior)
        Assert.That(score, Is.EqualTo(1.0), "Should use Total field when available, ignoring SubTotal+Tax calculation");
    }

    [Test]
    public void CalculateAmountScore_DocumentWithSubTotalOnly_UnchangedBehavior()
    {
        // Arrange - Document with only SubTotal (no Total, no Skonto)
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document 
        { 
            Total = null,
            SubTotal = 100.00m,
            TaxAmount = null,
            Skonto = null
        };

        // Act
        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Assert - Should use SubTotal (original fallback behavior)
        Assert.That(score, Is.EqualTo(1.0), "Should use SubTotal when Total is not available");
    }

    [Test]
    public void CalculateAmountScore_EdgeCasesWithoutSkonto_UnchangedBehavior()
    {
        // Test various edge cases to ensure they behave as before
        
        // Test 1: Null inputs
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 100.00m, Skonto = null };
        
        var scoreNullTransaction = _matcher.CalculateAmountScore(null, document, _config);
        Assert.That(scoreNullTransaction, Is.EqualTo(0.0), "Null transaction should return 0");

        var scoreNullDocument = _matcher.CalculateAmountScore(transaction, null, _config);
        Assert.That(scoreNullDocument, Is.EqualTo(0.0), "Null document should return 0");

        var scoreNullConfig = _matcher.CalculateAmountScore(transaction, document, null);
        Assert.That(scoreNullConfig, Is.EqualTo(0.0), "Null config should return 0");

        // Test 2: Document with zero amount (should return 0 as per original behavior)
        var documentZero = new Document { Total = 0.00m, Skonto = null };
        var scoreZero = _matcher.CalculateAmountScore(transaction, documentZero, _config);
        Assert.That(scoreZero, Is.EqualTo(0.0), "Document with zero amount should return 0 (original behavior)");
    }

    [Test]
    public void CalculateAmountScore_LargeAmounts_UnchangedPrecision()
    {
        // Arrange - Test with large amounts to ensure precision is maintained
        var transaction = new Transaction { GrossAmount = 1000000.00m };
        var document = new Document 
        { 
            Total = 1000500.00m, // 0.05% difference
            Skonto = null
        };

        // Act
        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Assert - Should handle large amounts with same precision as original
        Assert.That(score, Is.EqualTo(1.0), "Large amounts within tolerance should match exactly");
    }

    [Test]
    public void GetSkontoAdjustedAmount_BackwardCompatibilityMethod_StillWorks()
    {
        // Arrange - Test the deprecated method still works for backward compatibility
        var documentWithSkonto = new Document { Total = 100.00m, Skonto = 2.00m };
        var documentWithoutSkonto = new Document { Total = 100.00m, Skonto = null };

        // Act & Assert
        var adjustedWithSkonto = AmountMatcher.GetSkontoAdjustedAmount(documentWithSkonto);
        Assert.That(adjustedWithSkonto, Is.EqualTo(98.00m), "Method should still calculate Skonto correctly");

        var adjustedWithoutSkonto = AmountMatcher.GetSkontoAdjustedAmount(documentWithoutSkonto);
        Assert.That(adjustedWithoutSkonto, Is.Null, "Method should return null for documents without Skonto");
    }
}