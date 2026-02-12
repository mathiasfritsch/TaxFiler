using TaxFiler.DB.Model;
using TaxFiler.Service;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace TaxFiler.Service.Test;

[TestFixture]
public class VendorMatcherTests
{
    private VendorMatcher _matcher;
    private VendorMatchingConfig _config;

    [SetUp]
    public void SetUp()
    {
        _matcher = new VendorMatcher();
        _config = new VendorMatchingConfig
        {
            FuzzyMatchThreshold = 0.8
        };
    }

    [Test]
    public void CalculateVendorScore_ExactMatch_ReturnsOne()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH" };
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateVendorScore_ExactMatchIgnoreCase_ReturnsOne()
    {
        var transaction = new Transaction { Counterparty = "rewe markt gmbh" };
        var document = new Document { VendorName = "REWE MARKT GMBH" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateVendorScore_CounterpartyContainsVendor_ReturnsHighScore()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH & Co. KG" };
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.8));
    }

    [Test]
    public void CalculateVendorScore_VendorContainsCounterparty_ReturnsMediumHighScore()
    {
        var transaction = new Transaction { Counterparty = "REWE" };
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.7));
    }

    [Test]
    public void CalculateVendorScore_FuzzyMatchAboveThreshold_ReturnsFuzzyScore()
    {
        var transaction = new Transaction { Counterparty = "Mueller Drogerie" };
        var document = new Document { VendorName = "Müller Drogeriemarkt" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.GreaterThan(0.6).And.LessThan(0.9));
    }

    [Test]
    public void CalculateVendorScore_UsesSenderReceiverField()
    {
        var transaction = new Transaction 
        { 
            Counterparty = "Different Company",
            SenderReceiver = "REWE Markt GmbH" 
        };
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateVendorScore_ReturnsHighestScore()
    {
        var transaction = new Transaction 
        { 
            Counterparty = "REWE", // Would get 0.7 (vendor contains counterparty)
            SenderReceiver = "REWE Markt GmbH" // Would get 1.0 (exact match)
        };
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0)); // Should return the highest score
    }

    [Test]
    public void CalculateVendorScore_NullTransaction_ReturnsZero()
    {
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(null, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateVendorScore_NullDocument_ReturnsZero()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, null, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateVendorScore_NullConfig_ReturnsZero()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH" };
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, document, null);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateVendorScore_EmptyVendorName_ReturnsZero()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH" };
        var document = new Document { VendorName = "" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateVendorScore_NullVendorName_ReturnsZero()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH" };
        var document = new Document { VendorName = null };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateVendorScore_EmptyCounterpartyFields_ReturnsZero()
    {
        var transaction = new Transaction 
        { 
            Counterparty = "",
            SenderReceiver = null 
        };
        var document = new Document { VendorName = "REWE Markt GmbH" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateVendorScore_PartialWordMatch_ReturnsLowScore()
    {
        var transaction = new Transaction { Counterparty = "Deutsche Bank AG" };
        var document = new Document { VendorName = "Deutsche Post DHL" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.GreaterThan(0.0).And.LessThan(0.5));
    }

    [Test]
    public void CalculateVendorScore_NoMatch_ReturnsZero()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH" };
        var document = new Document { VendorName = "Lidl Stiftung & Co. KG" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void GetTransactionVendorFields_ReturnsNonEmptyFields()
    {
        var transaction = new Transaction 
        { 
            Counterparty = "REWE Markt GmbH",
            SenderReceiver = "REWE Group" 
        };

        var fields = VendorMatcher.GetTransactionVendorFields(transaction);

        Assert.That(fields, Has.Count.EqualTo(2));
        Assert.That(fields, Contains.Item("REWE Markt GmbH"));
        Assert.That(fields, Contains.Item("REWE Group"));
    }

    [Test]
    public void GetTransactionVendorFields_SkipsEmptyFields()
    {
        var transaction = new Transaction 
        { 
            Counterparty = "REWE Markt GmbH",
            SenderReceiver = "" 
        };

        var fields = VendorMatcher.GetTransactionVendorFields(transaction);

        Assert.That(fields, Has.Count.EqualTo(1));
        Assert.That(fields, Contains.Item("REWE Markt GmbH"));
    }

    [Test]
    public void GetTransactionVendorFields_AllEmpty_ReturnsEmptyList()
    {
        var transaction = new Transaction 
        { 
            SenderReceiver = "" 
        };

        var fields = VendorMatcher.GetTransactionVendorFields(transaction);

        Assert.That(fields, Is.Empty);
    }

    [Test]
    public void AreLikelySameVendor_SimilarNames_ReturnsTrue()
    {
        var result = VendorMatcher.AreLikelySameVendor("REWE Markt GmbH", "REWE Markt", 0.7);

        Assert.That(result, Is.True);
    }

    [Test]
    public void AreLikelySameVendor_DifferentNames_ReturnsFalse()
    {
        var result = VendorMatcher.AreLikelySameVendor("REWE Markt GmbH", "Lidl Stiftung", 0.7);

        Assert.That(result, Is.False);
    }

    [Test]
    public void AreLikelySameVendor_NullInputs_ReturnsFalse()
    {
        Assert.That(VendorMatcher.AreLikelySameVendor(null, "REWE", 0.7), Is.False);
        Assert.That(VendorMatcher.AreLikelySameVendor("REWE", null, 0.7), Is.False);
        Assert.That(VendorMatcher.AreLikelySameVendor(null, null, 0.7), Is.False);
    }

    [Test]
    public void CalculateVendorScore_GermanCharacters_HandledCorrectly()
    {
        var transaction = new Transaction { Counterparty = "Müller Drogeriemarkt" };
        var document = new Document { VendorName = "Mueller Drogeriemarkt" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.GreaterThan(0.8)); // Should handle German characters well
    }

    [Test]
    public void CalculateVendorScore_CommonGermanBusinessSuffixes_HandledCorrectly()
    {
        var transaction = new Transaction { Counterparty = "REWE Markt GmbH" };
        var document = new Document { VendorName = "REWE Markt GmbH & Co. KG" };

        var score = _matcher.CalculateVendorScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.7)); // Document contains transaction counterparty
    }
}