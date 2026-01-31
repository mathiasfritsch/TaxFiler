using TaxFiler.DB.Model;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class AmountMatcherTests
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
    public void CalculateAmountScore_ExactMatch_ReturnsOne()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 100.00m };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateAmountScore_WithinExactTolerance_ReturnsOne()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 100.50m }; // 0.5% difference

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateAmountScore_WithinHighTolerance_ReturnsHighScore()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 103.00m }; // 3% difference

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.8));
    }

    [Test]
    public void CalculateAmountScore_WithinMediumTolerance_ReturnsMediumScore()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 108.00m }; // 8% difference

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.5));
    }

    [Test]
    public void CalculateAmountScore_BeyondMediumTolerance_ReturnsLowScore()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 120.00m }; // 20% difference

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.GreaterThan(0.0).And.LessThan(0.2));
    }

    [Test]
    public void CalculateAmountScore_VeryLargeDifference_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 500.00m }; // 400% difference

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateAmountScore_NullTransaction_ReturnsZero()
    {
        var document = new Document { Total = 100.00m };

        var score = _matcher.CalculateAmountScore(null, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateAmountScore_NullDocument_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };

        var score = _matcher.CalculateAmountScore(transaction, null, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateAmountScore_NullConfig_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 100.00m };

        var score = _matcher.CalculateAmountScore(transaction, document, null);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateAmountScore_DocumentWithZeroAmount_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 0.00m };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateAmountScore_DocumentWithNullTotal_UsesSubTotal()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = null, SubTotal = 100.00m };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateAmountScore_DocumentWithSubTotalAndTax_UsesCalculatedTotal()
    {
        var transaction = new Transaction { GrossAmount = 119.00m };
        var document = new Document 
        { 
            Total = null, 
            SubTotal = 100.00m, 
            TaxAmount = 19.00m 
        };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateAmountScore_DocumentWithOnlyTaxAmount_UsesTaxAmount()
    {
        var transaction = new Transaction { GrossAmount = 19.00m };
        var document = new Document 
        { 
            Total = null, 
            SubTotal = null, 
            TaxAmount = 19.00m 
        };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateAmountScore_NegativeAmounts_HandledCorrectly()
    {
        var transaction = new Transaction { GrossAmount = -100.00m };
        var document = new Document { Total = -100.00m };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateAmountScore_MixedSignAmounts_HandledCorrectly()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = -100.00m };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0)); // 200% difference
    }

    [Test]
    public void GetSkontoAdjustedAmount_WithSkonto_ReturnsAdjustedAmount()
    {
        var document = new Document 
        { 
            Total = 100.00m, 
            Skonto = 2.00m 
        };

        var adjustedAmount = AmountMatcher.GetSkontoAdjustedAmount(document);

        Assert.That(adjustedAmount, Is.EqualTo(98.00m));
    }

    [Test]
    public void GetSkontoAdjustedAmount_WithoutSkonto_ReturnsNull()
    {
        var document = new Document { Total = 100.00m };

        var adjustedAmount = AmountMatcher.GetSkontoAdjustedAmount(document);

        Assert.That(adjustedAmount, Is.Null);
    }

    [Test]
    public void GetSkontoAdjustedAmount_WithZeroSkonto_ReturnsNull()
    {
        var document = new Document 
        { 
            Total = 100.00m, 
            Skonto = 0.00m 
        };

        var adjustedAmount = AmountMatcher.GetSkontoAdjustedAmount(document);

        Assert.That(adjustedAmount, Is.Null);
    }

    // Enhanced error handling tests for Skonto calculations

    [Test]
    public void CalculateAmountScore_DocumentWithExtremeSkonto_HandlesGracefully()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document 
        { 
            Total = 100.00m, 
            Skonto = decimal.MaxValue // Extreme Skonto percentage
        };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Should handle gracefully and return a valid score
        Assert.That(score, Is.GreaterThanOrEqualTo(0.0).And.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void CalculateAmountScore_DocumentWithInvalidSkontoResult_FallsBackToOriginal()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document 
        { 
            Total = 100.00m, 
            Skonto = 150.00m // 150% Skonto - would result in negative amount
        };

        var score = _matcher.CalculateAmountScore(transaction, document, _config);

        // Should handle the negative result gracefully and still provide a reasonable score
        Assert.That(score, Is.GreaterThanOrEqualTo(0.0).And.LessThanOrEqualTo(1.0));
    }

    #region Multiple Document Amount Tests

    [Test]
    public void CalculateMultipleAmountScore_ExactMatch_ReturnsOne()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 50.00m }
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateMultipleAmountScore_WithinExactTolerance_ReturnsOne()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 100.25m }, // Small difference
            new Document { Total = 50.25m }
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateMultipleAmountScore_WithinHighTolerance_ReturnsHighScore()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 102.00m }, // 3% total difference
            new Document { Total = 52.50m }
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(0.8));
    }

    [Test]
    public void CalculateMultipleAmountScore_WithinMediumTolerance_ReturnsMediumScore()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 105.00m }, // 8% total difference
            new Document { Total = 57.00m }
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(0.5));
    }

    [Test]
    public void CalculateMultipleAmountScore_BeyondMediumTolerance_ReturnsLowScore()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 120.00m }, // 20% total difference
            new Document { Total = 60.00m }
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.GreaterThan(0.0).And.LessThan(0.2));
    }

    [Test]
    public void CalculateMultipleAmountScore_VeryLargeDifference_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 300.00m }, // 400% total difference
            new Document { Total = 300.00m }
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateMultipleAmountScore_EmptyDocuments_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new Document[0];

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateMultipleAmountScore_NullDocuments_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };

        var score = _matcher.CalculateMultipleAmountScore(transaction, null, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateMultipleAmountScore_NullTransaction_ReturnsZero()
    {
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 50.00m }
        };

        var score = _matcher.CalculateMultipleAmountScore(null, documents, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateMultipleAmountScore_NullConfig_ReturnsZero()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 50.00m }
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, null);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateMultipleAmountScore_WithSkonto_AppliesDiscountCorrectly()
    {
        var transaction = new Transaction { GrossAmount = 147.00m }; // Expecting 2% Skonto on 150
        var documents = new[]
        {
            new Document { Total = 100.00m, Skonto = 2.00m }, // 98 after Skonto
            new Document { Total = 50.00m, Skonto = 2.00m }   // 49 after Skonto
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(1.0)); // 98 + 49 = 147, exact match
    }

    [Test]
    public void CalculateMultipleAmountScore_MixedSkontoAndNormal_HandlesCorrectly()
    {
        var transaction = new Transaction { GrossAmount = 148.00m };
        var documents = new[]
        {
            new Document { Total = 100.00m, Skonto = 2.00m }, // 98 after Skonto
            new Document { Total = 50.00m }                   // 50 no Skonto
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(1.0)); // 98 + 50 = 148, exact match
    }

    [Test]
    public void CalculateMultipleAmountScore_DocumentsWithZeroAmounts_IgnoresZeros()
    {
        var transaction = new Transaction { GrossAmount = 100.00m };
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 0.00m },    // Should be ignored
            new Document { Total = null }      // Should be ignored
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(1.0)); // Only 100 is counted
    }

    [Test]
    public void CalculateMultipleAmountScore_DocumentsWithDifferentAmountFields_UsesBestAmount()
    {
        var transaction = new Transaction { GrossAmount = 150.00m };
        var documents = new[]
        {
            new Document { Total = 100.00m },                           // Uses Total
            new Document { Total = null, SubTotal = 40.00m, TaxAmount = 10.00m }, // Uses SubTotal + Tax = 50
        };

        var score = _matcher.CalculateMultipleAmountScore(transaction, documents, _config);

        Assert.That(score, Is.EqualTo(1.0)); // 100 + 50 = 150, exact match
    }

    #endregion

    #region Multiple Amount Validation Tests

    [Test]
    public void ValidateMultipleAmounts_ExactMatch_ReturnsValid()
    {
        var transactionAmount = 150.00m;
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 50.00m }
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(150.00m));
        Assert.That(result.TransactionAmount, Is.EqualTo(150.00m));
        Assert.That(result.AmountDifference, Is.EqualTo(0.00m));
        Assert.That(result.PercentageDifference, Is.EqualTo(0.0));
        Assert.That(result.ValidDocumentCount, Is.EqualTo(2));
        Assert.That(result.SkontoAppliedCount, Is.EqualTo(0));
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Recommendations, Is.Empty);
    }

    [Test]
    public void ValidateMultipleAmounts_MinorOverage_ReturnsValidWithWarning()
    {
        var transactionAmount = 150.00m;
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 58.00m } // 8/150 = 5.3% overage
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.True); // Still valid for minor overage
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(158.00m));
        Assert.That(result.AmountDifference, Is.EqualTo(8.00m));
        Assert.That(result.PercentageDifference, Is.GreaterThan(0.05).And.LessThan(0.06));
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Contains.Substring("slightly exceeds"));
        Assert.That(result.Recommendations, Has.Count.EqualTo(1));
    }

    [Test]
    public void ValidateMultipleAmounts_SignificantOverage_ReturnsInvalidWithWarnings()
    {
        var transactionAmount = 150.00m;
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 80.00m } // 30/180 = 16.7% overage
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.HasSignificantOverage, Is.True);
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(180.00m));
        Assert.That(result.PercentageDifference, Is.GreaterThan(0.15));
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Contains.Substring("significantly exceeds"));
        Assert.That(result.Recommendations, Has.Count.GreaterThan(1));
        Assert.That(result.Recommendations.Any(r => r.Contains("different transactions")), Is.True);
    }

    [Test]
    public void ValidateMultipleAmounts_SignificantUnderage_ReturnsInvalidWithWarnings()
    {
        var transactionAmount = 150.00m;
        var documents = new[]
        {
            new Document { Total = 60.00m },
            new Document { Total = 50.00m } // 40/150 = 26.7% underage
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.HasSignificantUnderage, Is.True);
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(110.00m));
        Assert.That(result.PercentageDifference, Is.GreaterThan(0.25));
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Contains.Substring("significantly less"));
        Assert.That(result.Recommendations, Has.Count.GreaterThan(1));
        Assert.That(result.Recommendations.Any(r => r.Contains("missing")), Is.True);
    }

    [Test]
    public void ValidateMultipleAmounts_WithSkonto_IncludesSkontoInformation()
    {
        var transactionAmount = 147.00m;
        var documents = new[]
        {
            new Document { Total = 100.00m, Skonto = 2.00m }, // 98 after Skonto
            new Document { Total = 50.00m, Skonto = 2.00m }   // 49 after Skonto
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(147.00m)); // 98 + 49
        Assert.That(result.SkontoAppliedCount, Is.EqualTo(2));
        Assert.That(result.Recommendations.Any(r => r.Contains("Skonto")), Is.True);
    }

    [Test]
    public void ValidateMultipleAmounts_EmptyDocuments_ReturnsInvalid()
    {
        var transactionAmount = 150.00m;
        var documents = new Document[0];

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ValidDocumentCount, Is.EqualTo(0));
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Contains.Substring("empty"));
    }

    [Test]
    public void ValidateMultipleAmounts_NullDocuments_ReturnsInvalid()
    {
        var transactionAmount = 150.00m;

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.Warnings[0], Contains.Substring("No documents provided"));
    }

    [Test]
    public void ValidateMultipleAmounts_DocumentsWithNoValidAmounts_ReturnsInvalid()
    {
        var transactionAmount = 150.00m;
        var documents = new[]
        {
            new Document { Total = null, SubTotal = null, TaxAmount = null },
            new Document { Total = 0.00m }
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ValidDocumentCount, Is.EqualTo(0));
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(0.00m));
        Assert.That(result.Warnings.Any(w => w.Contains("No documents have valid amounts")), Is.True);
    }

    [Test]
    public void ValidateMultipleAmounts_NegativeTransactionAmount_HandlesCorrectly()
    {
        var transactionAmount = -150.00m; // Should use absolute value
        var documents = new[]
        {
            new Document { Total = 100.00m },
            new Document { Total = 50.00m }
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.TransactionAmount, Is.EqualTo(150.00m)); // Absolute value used
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(150.00m));
        Assert.That(result.AmountDifference, Is.EqualTo(0.00m));
    }

    [Test]
    public void ValidateMultipleAmounts_InvalidSkontoCalculation_FallsBackToOriginal()
    {
        var transactionAmount = 150.00m;
        var documents = new[]
        {
            new Document { Total = 100.00m, Skonto = 150.00m }, // Invalid 150% Skonto
            new Document { Total = 50.00m }
        };

        var result = _matcher.ValidateMultipleAmounts(transactionAmount, documents);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.TotalDocumentAmount, Is.EqualTo(150.00m)); // Should use original amounts
        Assert.That(result.SkontoAppliedCount, Is.EqualTo(0)); // Invalid Skonto not applied
    }

    #endregion
}