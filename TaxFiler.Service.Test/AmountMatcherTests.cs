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
}