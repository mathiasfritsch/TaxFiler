using TaxFiler.DB.Model;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class DateMatcherTests
{
    private DateMatcher _matcher;
    private DateMatchingConfig _config;

    [SetUp]
    public void SetUp()
    {
        _matcher = new DateMatcher();
        _config = new DateMatchingConfig
        {
            ExactMatchDays = 0,
            HighMatchDays = 7,
            MediumMatchDays = 30
        };
    }

    [Test]
    public void CalculateDateScore_ExactMatch_ReturnsOne()
    {
        var date = new DateTime(2024, 1, 15);
        var transaction = new Transaction { TransactionDateTime = date };
        var document = new Document { InvoiceDate = DateOnly.FromDateTime(date) };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateDateScore_WithinHighTolerance_ReturnsHighScore()
    {
        var transactionDate = new DateTime(2024, 1, 15);
        var invoiceDate = DateOnly.FromDateTime(new DateTime(2024, 1, 18)); // 3 days difference
        
        var transaction = new Transaction { TransactionDateTime = transactionDate };
        var document = new Document { InvoiceDate = invoiceDate };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.8));
    }

    [Test]
    public void CalculateDateScore_WithinMediumTolerance_ReturnsMediumScore()
    {
        var transactionDate = new DateTime(2024, 1, 15);
        var invoiceDate = DateOnly.FromDateTime(new DateTime(2024, 1, 30)); // 15 days difference
        
        var transaction = new Transaction { TransactionDateTime = transactionDate };
        var document = new Document { InvoiceDate = invoiceDate };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.5));
    }

    [Test]
    public void CalculateDateScore_BeyondMediumTolerance_ReturnsLowScore()
    {
        var transactionDate = new DateTime(2024, 1, 15);
        var invoiceDate = DateOnly.FromDateTime(new DateTime(2024, 2, 20)); // 36 days difference
        
        var transaction = new Transaction { TransactionDateTime = transactionDate };
        var document = new Document { InvoiceDate = invoiceDate };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.GreaterThan(0.0).And.LessThan(0.2));
    }

    [Test]
    public void CalculateDateScore_VeryLargeDifference_ReturnsZero()
    {
        var transactionDate = new DateTime(2024, 1, 15);
        var invoiceDate = DateOnly.FromDateTime(new DateTime(2024, 6, 15)); // 152 days difference
        
        var transaction = new Transaction { TransactionDateTime = transactionDate };
        var document = new Document { InvoiceDate = invoiceDate };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateDateScore_NullTransaction_ReturnsZero()
    {
        var document = new Document { InvoiceDate = DateOnly.FromDateTime(DateTime.Now) };

        var score = _matcher.CalculateDateScore(null, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateDateScore_NullDocument_ReturnsZero()
    {
        var transaction = new Transaction { TransactionDateTime = DateTime.Now };

        var score = _matcher.CalculateDateScore(transaction, null, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateDateScore_NullConfig_ReturnsZero()
    {
        var transaction = new Transaction { TransactionDateTime = DateTime.Now };
        var document = new Document { InvoiceDate = DateOnly.FromDateTime(DateTime.Now) };

        var score = _matcher.CalculateDateScore(transaction, document, null);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateDateScore_DocumentWithNullInvoiceDate_ReturnsZero()
    {
        var transaction = new Transaction { TransactionDateTime = DateTime.Now };
        var document = new Document { InvoiceDate = null };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateDateScore_UsesInvoiceDateFromFolderAsFallback()
    {
        var date = new DateTime(2024, 1, 15);
        var transaction = new Transaction { TransactionDateTime = date };
        var document = new Document 
        { 
            InvoiceDate = null, 
            InvoiceDateFromFolder = DateOnly.FromDateTime(date) 
        };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateDateScore_PrefersInvoiceDateOverFolder()
    {
        var transactionDate = new DateTime(2024, 1, 15);
        var invoiceDate = DateOnly.FromDateTime(new DateTime(2024, 1, 15));
        var folderDate = DateOnly.FromDateTime(new DateTime(2024, 2, 15)); // 31 days difference
        
        var transaction = new Transaction { TransactionDateTime = transactionDate };
        var document = new Document 
        { 
            InvoiceDate = invoiceDate,
            InvoiceDateFromFolder = folderDate 
        };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(1.0)); // Should use InvoiceDate, not folder date
    }

    [Test]
    public void CalculateDateScore_PastDate_HandledCorrectly()
    {
        var transactionDate = new DateTime(2024, 1, 20);
        var invoiceDate = DateOnly.FromDateTime(new DateTime(2024, 1, 15)); // 5 days before
        
        var transaction = new Transaction { TransactionDateTime = transactionDate };
        var document = new Document { InvoiceDate = invoiceDate };

        var score = _matcher.CalculateDateScore(transaction, document, _config);

        Assert.That(score, Is.EqualTo(0.8)); // Within high tolerance
    }

    [Test]
    public void CalculateDaysDifference_ReturnsCorrectDifference()
    {
        var date1 = new DateOnly(2024, 1, 15);
        var date2 = new DateOnly(2024, 1, 20);

        var difference = DateMatcher.CalculateDaysDifference(date1, date2);

        Assert.That(difference, Is.EqualTo(5));
    }

    [Test]
    public void CalculateDaysDifference_SameDate_ReturnsZero()
    {
        var date = new DateOnly(2024, 1, 15);

        var difference = DateMatcher.CalculateDaysDifference(date, date);

        Assert.That(difference, Is.EqualTo(0));
    }

    [Test]
    public void AreWithinTolerance_WithinTolerance_ReturnsTrue()
    {
        var date1 = new DateOnly(2024, 1, 15);
        var date2 = new DateOnly(2024, 1, 18);

        var result = DateMatcher.AreWithinTolerance(date1, date2, 5);

        Assert.That(result, Is.True);
    }

    [Test]
    public void AreWithinTolerance_BeyondTolerance_ReturnsFalse()
    {
        var date1 = new DateOnly(2024, 1, 15);
        var date2 = new DateOnly(2024, 1, 25);

        var result = DateMatcher.AreWithinTolerance(date1, date2, 5);

        Assert.That(result, Is.False);
    }

    [Test]
    public void GetAlternativeDocumentDate_WithBothDates_ReturnsFolder()
    {
        var invoiceDate = new DateOnly(2024, 1, 15);
        var folderDate = new DateOnly(2024, 1, 20);
        
        var document = new Document 
        { 
            InvoiceDate = invoiceDate,
            InvoiceDateFromFolder = folderDate 
        };

        var alternativeDate = DateMatcher.GetAlternativeDocumentDate(document);

        Assert.That(alternativeDate, Is.EqualTo(folderDate));
    }

    [Test]
    public void GetAlternativeDocumentDate_WithOnlyInvoiceDate_ReturnsNull()
    {
        var document = new Document 
        { 
            InvoiceDate = new DateOnly(2024, 1, 15),
            InvoiceDateFromFolder = null 
        };

        var alternativeDate = DateMatcher.GetAlternativeDocumentDate(document);

        Assert.That(alternativeDate, Is.Null);
    }
}