using TaxFiler.DB.Model;
using TaxFiler.Service;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace TaxFiler.Service.Test;

[TestFixture]
public class ReferenceMatcherTests
{
    private ReferenceMatcher _matcher;

    [SetUp]
    public void SetUp()
    {
        _matcher = new ReferenceMatcher();
    }

    [Test]
    public void CalculateReferenceScore_ExactMatch_ReturnsOne()
    {
        var transaction = new Transaction { TransactionNote = "INV-2024-001" };
        var document = new Document { InvoiceNumber = "INV-2024-001" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateReferenceScore_ExactMatchIgnoreCase_ReturnsOne()
    {
        var transaction = new Transaction { TransactionNote = "inv-2024-001" };
        var document = new Document { InvoiceNumber = "INV-2024-001" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateReferenceScore_TransactionContainsInvoice_ReturnsHighScore()
    {
        var transaction = new Transaction { TransactionNote = "Payment for INV-2024-001" };
        var document = new Document { InvoiceNumber = "INV-2024-001" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.8));
    }

    [Test]
    public void CalculateReferenceScore_InvoiceContainsTransaction_ReturnsMediumHighScore()
    {
        var transaction = new Transaction { TransactionNote = "2024-001" };
        var document = new Document { InvoiceNumber = "INV-2024-001-EXTRA" }; // Added extra text to ensure it's not exact match after normalization

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.7));
    }

    [Test]
    public void CalculateReferenceScore_NumericMatch_ReturnsNumericScore()
    {
        var transaction = new Transaction { TransactionNote = "12345" };
        var document = new Document { InvoiceNumber = "ABC-67890-DEF" }; // Different numbers, should use numeric matching

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.0)); // No numeric match expected
    }

    [Test]
    public void CalculateReferenceScore_ActualNumericMatch_ReturnsNumericScore()
    {
        var transaction = new Transaction { TransactionNote = "ABC-12345-XYZ" };
        var document = new Document { InvoiceNumber = "DEF-12345-GHI" }; // Same number in different contexts

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.GreaterThan(0.0).And.LessThanOrEqualTo(0.6));
    }

    [Test]
    public void CalculateReferenceScore_PatternMatch_ReturnsPatternScore()
    {
        var transaction = new Transaction { TransactionNote = "ABC-123-DEF" };
        var document = new Document { InvoiceNumber = "XYZ-456-GHI" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.GreaterThan(0.0).And.LessThanOrEqualTo(0.4));
    }

    [Test]
    public void CalculateReferenceScore_NoMatch_ReturnsZero()
    {
        var transaction = new Transaction { TransactionNote = "ABCDEF" };
        var document = new Document { InvoiceNumber = "123456" }; // Completely different patterns

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.0));
    }
    
    [Test]
    public void CalculateReferenceScore_EmptyTransactionReference_ReturnsZero()
    {
        var transaction = new Transaction { TransactionNote = "" };
        var document = new Document { InvoiceNumber = "INV-2024-001" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateReferenceScore_NullTransactionReference_ReturnsZero()
    {
        var transaction = new Transaction { TransactionNote = null };
        var document = new Document { InvoiceNumber = "INV-2024-001" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateReferenceScore_EmptyInvoiceNumber_ReturnsZero()
    {
        var transaction = new Transaction { TransactionNote = "INV-2024-001" };
        var document = new Document { InvoiceNumber = "" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateReferenceScore_NullInvoiceNumber_ReturnsZero()
    {
        var transaction = new Transaction { TransactionNote = "INV-2024-001" };
        var document = new Document { InvoiceNumber = null };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateReferenceScore_WhitespaceReferences_ReturnsZero()
    {
        var transaction = new Transaction { TransactionReference = "   " };
        var document = new Document { InvoiceNumber = "   " };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateReferenceScore_HandlesCommonPrefixes()
    {
        var transaction = new Transaction { TransactionNote = "Invoice 2024-001" };
        var document = new Document { InvoiceNumber = "INV 2024-001" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.GreaterThan(0.7)); // Should normalize and match well
    }

    [Test]
    public void CalculateReferenceScore_HandlesDifferentSeparators()
    {
        var transaction = new Transaction { TransactionNote = "2024/001/ABC" };
        var document = new Document { InvoiceNumber = "2024-001-ABC" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.GreaterThan(0.8)); // Should normalize separators
    }

    [Test]
    public void CalculateReferenceScore_SimilarNumericReferences_ReturnsScore()
    {
        var transaction = new Transaction { TransactionNote = "12345" };
        var document = new Document { InvoiceNumber = "12346" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.GreaterThan(0.0)); // Should detect similar numbers
    }

    [Test]
    public void IsValidReference_ValidReferences_ReturnsTrue()
    {
        Assert.That(ReferenceMatcher.IsValidReference("INV-2024-001"), Is.True);
        Assert.That(ReferenceMatcher.IsValidReference("12345"), Is.True);
        Assert.That(ReferenceMatcher.IsValidReference("ABC123"), Is.True);
    }

    [Test]
    public void IsValidReference_InvalidReferences_ReturnsFalse()
    {
        Assert.That(ReferenceMatcher.IsValidReference(null), Is.False);
        Assert.That(ReferenceMatcher.IsValidReference(""), Is.False);
        Assert.That(ReferenceMatcher.IsValidReference("   "), Is.False);
        Assert.That(ReferenceMatcher.IsValidReference("AB"), Is.False); // Too short
        Assert.That(ReferenceMatcher.IsValidReference("N/A"), Is.False);
        Assert.That(ReferenceMatcher.IsValidReference("NULL"), Is.False);
        Assert.That(ReferenceMatcher.IsValidReference("UNKNOWN"), Is.False);
    }

    [Test]
    public void IsValidReference_SpecialCharactersOnly_ReturnsFalse()
    {
        Assert.That(ReferenceMatcher.IsValidReference("---"), Is.False);
        Assert.That(ReferenceMatcher.IsValidReference("..."), Is.False);
        Assert.That(ReferenceMatcher.IsValidReference("///"), Is.False);
    }

    [Test]
    public void CalculateReferenceScore_LongNumericReferences_HigherScore()
    {
        var transaction = new Transaction { TransactionNote = "1234567890" };
        var document = new Document { InvoiceNumber = "INV-1234567890-2024" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.GreaterThan(0.5)); // Longer numbers should score higher
    }

    [Test]
    public void CalculateReferenceScore_ShortNumericReferences_LowerScore()
    {
        var transaction = new Transaction { TransactionReference = "AB" }; // Very short, no numbers
        var document = new Document { InvoiceNumber = "INV-12-2024" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        // Short references with no meaningful content should not match
        Assert.That(score, Is.LessThan(0.5));
    }

    [Test]
    public void CalculateReferenceScore_GermanInvoiceFormats_HandledCorrectly()
    {
        var transaction = new Transaction { TransactionNote = "Rechnung Nr. 2024-001" };
        var document = new Document { InvoiceNumber = "RG-2024-001" };

        var score = _matcher.CalculateReferenceScore(transaction, document);

        Assert.That(score, Is.GreaterThan(0.3)); // Should handle German invoice formats with some similarity
    }
}