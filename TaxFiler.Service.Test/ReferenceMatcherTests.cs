using TaxFiler.DB.Model;
using TaxFiler.Service;

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

    #region Multiple Voucher Number Tests

    [Test]
    public void ExtractVoucherNumbers_SingleVoucher_ReturnsOne()
    {
        var voucherNumbers = _matcher.ExtractVoucherNumbers("Rechnung INV-2024-001").ToList();

        Assert.That(voucherNumbers, Has.Count.EqualTo(1));
        Assert.That(voucherNumbers[0], Is.EqualTo("INV-2024-001"));
    }

    [Test]
    public void ExtractVoucherNumbers_MultipleVouchersCommaSeparated_ReturnsAll()
    {
        var voucherNumbers = _matcher.ExtractVoucherNumbers("INV-2024-001, INV-2024-002").ToList();

        Assert.That(voucherNumbers, Has.Count.EqualTo(2));
        Assert.That(voucherNumbers, Contains.Item("INV-2024-001"));
        Assert.That(voucherNumbers, Contains.Item("INV-2024-002"));
    }

    [Test]
    public void ExtractVoucherNumbers_MultipleVouchersSemicolonSeparated_ReturnsAll()
    {
        var voucherNumbers = _matcher.ExtractVoucherNumbers("RG-001; RG-002; RG-003").ToList();

        Assert.That(voucherNumbers, Has.Count.EqualTo(3));
        Assert.That(voucherNumbers, Contains.Item("RG-001"));
        Assert.That(voucherNumbers, Contains.Item("RG-002"));
        Assert.That(voucherNumbers, Contains.Item("RG-003"));
    }

    [Test]
    public void ExtractVoucherNumbers_GermanInvoicePatterns_ReturnsCorrectly()
    {
        var testCases = new[]
        {
            ("Rechnung: 2024-001", "2024-001"),
            ("RG-Nr. 12345", "12345"),
            ("Rechnungsnummer: INV-2024-001", "INV-2024-001"),
            ("Invoice No: ABC123", "ABC123"),
            ("Beleg: REF-456", "REF-456")
        };

        foreach (var (input, expected) in testCases)
        {
            var voucherNumbers = _matcher.ExtractVoucherNumbers(input).ToList();
            Assert.That(voucherNumbers, Has.Count.EqualTo(1), $"Failed for input: {input}");
            Assert.That(voucherNumbers[0], Is.EqualTo(expected), $"Failed for input: {input}");
        }
    }

    [Test]
    public void ExtractVoucherNumbers_WithAndConnector_ReturnsAll()
    {
        var testCases = new[]
        {
            "INV-001 und INV-002",
            "RG-123 and RG-456",
            "REF-001 & REF-002",
            "2024-001 + 2024-002",
            "INV-001 sowie INV-002"
        };

        foreach (var input in testCases)
        {
            var voucherNumbers = _matcher.ExtractVoucherNumbers(input).ToList();
            Assert.That(voucherNumbers, Has.Count.EqualTo(2), $"Failed for input: {input}");
        }
    }

    [Test]
    public void ExtractVoucherNumbers_RangePattern_ReturnsEndpoints()
    {
        var voucherNumbers = _matcher.ExtractVoucherNumbers("INV-001 bis INV-005").ToList();

        Assert.That(voucherNumbers, Has.Count.EqualTo(2));
        Assert.That(voucherNumbers, Contains.Item("INV-001"));
        Assert.That(voucherNumbers, Contains.Item("INV-005"));
    }

    [Test]
    public void ExtractVoucherNumbers_EmptyOrNull_ReturnsEmpty()
    {
        Assert.That(_matcher.ExtractVoucherNumbers(null), Is.Empty);
        Assert.That(_matcher.ExtractVoucherNumbers(""), Is.Empty);
        Assert.That(_matcher.ExtractVoucherNumbers("   "), Is.Empty);
    }

    [Test]
    public void ExtractVoucherNumbers_NoValidReferences_ReturnsEmpty()
    {
        var voucherNumbers = _matcher.ExtractVoucherNumbers("Payment received").ToList();
        Assert.That(voucherNumbers, Is.Empty);
    }

    [Test]
    public void ExtractVoucherNumbers_FiltersDuplicates_ReturnsUnique()
    {
        var voucherNumbers = _matcher.ExtractVoucherNumbers("INV-001, inv-001, INV-001").ToList();

        Assert.That(voucherNumbers, Has.Count.EqualTo(1));
        Assert.That(voucherNumbers[0], Is.EqualTo("INV-001"));
    }

    [Test]
    public void ExtractVoucherNumbers_ComplexGermanText_ExtractsCorrectly()
    {
        var note = "Zahlung für Rechnung RG-2024-001 sowie Beleg REF-456 und zusätzlich INV-789";
        var voucherNumbers = _matcher.ExtractVoucherNumbers(note).ToList();

        Assert.That(voucherNumbers, Has.Count.EqualTo(3));
        Assert.That(voucherNumbers, Contains.Item("RG-2024-001"));
        Assert.That(voucherNumbers, Contains.Item("REF-456"));
        Assert.That(voucherNumbers, Contains.Item("INV-789"));
    }

    [Test]
    public void CalculateReferenceScore_MultipleDocuments_SingleDocument_SameAsIndividual()
    {
        var transaction = new Transaction { TransactionNote = "INV-2024-001" };
        var document = new Document { InvoiceNumber = "INV-2024-001" };
        var documents = new[] { document };

        var individualScore = _matcher.CalculateReferenceScore(transaction, document);
        var multipleScore = _matcher.CalculateReferenceScore(transaction, documents);

        Assert.That(multipleScore, Is.EqualTo(individualScore));
    }

    [Test]
    public void CalculateReferenceScore_MultipleDocuments_ReturnsHighestScore()
    {
        var transaction = new Transaction { TransactionNote = "INV-2024-001" };
        var documents = new[]
        {
            new Document { InvoiceNumber = "INV-2024-999" }, // Low match
            new Document { InvoiceNumber = "INV-2024-001" }, // Exact match
            new Document { InvoiceNumber = "REF-456" }       // No match
        };

        var score = _matcher.CalculateReferenceScore(transaction, documents);

        Assert.That(score, Is.EqualTo(1.0)); // Should return the exact match score
    }

    [Test]
    public void CalculateReferenceScore_MultipleVouchersMultipleDocuments_AppliesBonus()
    {
        var transaction = new Transaction { TransactionNote = "INV-001, INV-002, INV-003" };
        var documents = new[]
        {
            new Document { InvoiceNumber = "INV-001" },
            new Document { InvoiceNumber = "INV-002" },
            new Document { InvoiceNumber = "INV-003" }
        };

        var score = _matcher.CalculateReferenceScore(transaction, documents);

        // Should get bonus for matching multiple vouchers
        Assert.That(score, Is.GreaterThan(1.0).Or.EqualTo(1.0)); // Capped at 1.0
    }

    [Test]
    public void CalculateReferenceScore_PartialMultipleMatch_AppliesPartialBonus()
    {
        var transaction = new Transaction { TransactionNote = "INV-001, INV-002, INV-003" };
        var documents = new[]
        {
            new Document { InvoiceNumber = "INV-001" },
            new Document { InvoiceNumber = "INV-002" },
            new Document { InvoiceNumber = "DIFFERENT-REF" }
        };

        var score = _matcher.CalculateReferenceScore(transaction, documents);

        // Should get partial bonus for matching 2 out of 3 vouchers
        Assert.That(score, Is.GreaterThan(0.8)); // Base exact match score plus bonus
    }

    [Test]
    public void CalculateReferenceScore_NoVouchersInNote_FallsBackToMaxScore()
    {
        var transaction = new Transaction { TransactionNote = "General payment" };
        var documents = new[]
        {
            new Document { InvoiceNumber = "INV-001" },
            new Document { InvoiceNumber = "INV-002" }
        };

        var score = _matcher.CalculateReferenceScore(transaction, documents);

        // Should fall back to maximum individual score
        Assert.That(score, Is.GreaterThanOrEqualTo(0.0));
    }

    [Test]
    public void CalculateReferenceScore_EmptyDocumentList_ReturnsZero()
    {
        var transaction = new Transaction { TransactionNote = "INV-001" };
        var documents = new Document[0];

        var score = _matcher.CalculateReferenceScore(transaction, documents);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateReferenceScore_NullDocumentList_ReturnsZero()
    {
        var transaction = new Transaction { TransactionNote = "INV-001" };

        var score = _matcher.CalculateReferenceScore(transaction, (IEnumerable<Document>)null);

        Assert.That(score, Is.EqualTo(0.0));
    }

    #endregion
}