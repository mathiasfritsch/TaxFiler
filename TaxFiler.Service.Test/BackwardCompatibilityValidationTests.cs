using TaxFiler.DB.Model;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

/// <summary>
/// Final validation tests to ensure complete backward compatibility.
/// These tests simulate real-world scenarios with documents that have no Skonto terms
/// to verify the enhanced implementation behaves identically to the original.
/// </summary>
[TestFixture]
public class BackwardCompatibilityValidationTests
{
    private AmountMatcher _matcher;
    private MatchingConfiguration _config;

    [SetUp]
    public void SetUp()
    {
        _matcher = new AmountMatcher();
        _config = new MatchingConfiguration();
    }

    [Test]
    public void DocumentMatching_RealWorldScenario_WithoutSkonto_WorksAsExpected()
    {
        // Arrange - Simulate a real transaction and multiple documents without Skonto
        var transaction = new Transaction 
        { 
            Id = 1,
            GrossAmount = 119.00m,
            TransactionReference = "TXN-001",
            TransactionDateTime = DateTime.Now
        };

        var documents = new[]
        {
            // Exact match
            new Document 
            { 
                Id = 1, 
                Total = 119.00m, 
                Skonto = null,
                VendorName = "Test Vendor"
            },
            // Close match (within high tolerance)
            new Document 
            { 
                Id = 2, 
                Total = 122.00m, 
                Skonto = null,
                VendorName = "Test Vendor"
            },
            // Medium match
            new Document 
            { 
                Id = 3, 
                Total = 130.00m, 
                Skonto = null,
                VendorName = "Test Vendor"
            },
            // Poor match (but still within scoring range)
            new Document 
            { 
                Id = 4, 
                Total = 150.00m, // ~26% difference, should still get some score
                Skonto = null,
                VendorName = "Test Vendor"
            }
        };

        // Act - Calculate scores for each document
        var scores = documents.Select(doc => 
            _matcher.CalculateAmountScore(transaction, doc, _config.AmountConfig)).ToArray();

        // Assert - Verify scoring behaves as expected for non-Skonto documents
        Assert.That(scores[0], Is.EqualTo(1.0), "Exact match should score 1.0");
        Assert.That(scores[1], Is.EqualTo(0.8), "Close match should score 0.8");
        Assert.That(scores[2], Is.EqualTo(0.5), "Medium match should score 0.5");
        Assert.That(scores[3], Is.GreaterThan(0.0).And.LessThan(0.2), "Poor match should score low but > 0");
    }

    [Test]
    public void DocumentMatching_VariousAmountFields_WithoutSkonto_PriorityUnchanged()
    {
        // Arrange - Test document amount field priority remains unchanged
        var transaction = new Transaction { GrossAmount = 119.00m };

        // Test 1: Document with Total field (highest priority)
        var docWithTotal = new Document 
        { 
            Total = 119.00m,
            SubTotal = 100.00m,
            TaxAmount = 19.00m,
            Skonto = null
        };

        // Test 2: Document without Total but with SubTotal + TaxAmount
        var docWithSubTotalAndTax = new Document 
        { 
            Total = null,
            SubTotal = 100.00m,
            TaxAmount = 19.00m,
            Skonto = null
        };

        // Test 3: Document with only SubTotal
        var docWithSubTotalOnly = new Document 
        { 
            Total = null,
            SubTotal = 119.00m,
            TaxAmount = null,
            Skonto = null
        };

        // Act
        var scoreTotal = _matcher.CalculateAmountScore(transaction, docWithTotal, _config.AmountConfig);
        var scoreSubTotalTax = _matcher.CalculateAmountScore(transaction, docWithSubTotalAndTax, _config.AmountConfig);
        var scoreSubTotalOnly = _matcher.CalculateAmountScore(transaction, docWithSubTotalOnly, _config.AmountConfig);

        // Assert - All should match perfectly, demonstrating unchanged priority logic
        Assert.That(scoreTotal, Is.EqualTo(1.0), "Total field should be used (highest priority)");
        Assert.That(scoreSubTotalTax, Is.EqualTo(1.0), "SubTotal + TaxAmount should be calculated when Total is null");
        Assert.That(scoreSubTotalOnly, Is.EqualTo(1.0), "SubTotal should be used when Total and TaxAmount are null");
    }

    [Test]
    public void DocumentMatching_GermanTaxScenarios_WithoutSkonto_WorksCorrectly()
    {
        // Arrange - Simulate typical German tax document scenarios without Skonto
        var transactions = new[]
        {
            new Transaction { GrossAmount = 119.00m }, // 100 + 19% VAT
            new Transaction { GrossAmount = 107.00m }, // 100 + 7% VAT
            new Transaction { GrossAmount = 100.00m }  // Net amount
        };

        var documents = new[]
        {
            // Standard VAT document
            new Document 
            { 
                Total = 119.00m,
                SubTotal = 100.00m,
                TaxAmount = 19.00m,
                Skonto = null
            },
            // Reduced VAT document
            new Document 
            { 
                Total = 107.00m,
                SubTotal = 100.00m,
                TaxAmount = 7.00m,
                Skonto = null
            },
            // Net amount document
            new Document 
            { 
                Total = 100.00m,
                SubTotal = 100.00m,
                TaxAmount = 0.00m,
                Skonto = null
            }
        };

        // Act & Assert - Each transaction should match its corresponding document perfectly
        for (int i = 0; i < transactions.Length; i++)
        {
            var score = _matcher.CalculateAmountScore(transactions[i], documents[i], _config.AmountConfig);
            Assert.That(score, Is.EqualTo(1.0), $"German tax scenario {i + 1} should match perfectly");
        }
    }

    [Test]
    public void PublicInterface_RemainsUnchanged()
    {
        // Arrange - Verify the public interface hasn't changed
        var transaction = new Transaction { GrossAmount = 100.00m };
        var document = new Document { Total = 100.00m, Skonto = null };

        // Act - Test that the interface method still works exactly as before
        IAmountMatcher interfaceMatcher = _matcher;
        var score = interfaceMatcher.CalculateAmountScore(transaction, document, _config.AmountConfig);

        // Assert - Interface should work identically
        Assert.That(score, Is.EqualTo(1.0), "Public interface should work identically to before");
        
        // Verify method signature hasn't changed
        var method = typeof(IAmountMatcher).GetMethod("CalculateAmountScore");
        Assert.That(method, Is.Not.Null, "CalculateAmountScore method should exist");
        Assert.That(method.ReturnType, Is.EqualTo(typeof(double)), "Return type should be double");
        
        var parameters = method.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(3), "Should have exactly 3 parameters");
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(Transaction)), "First parameter should be Transaction");
        Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(Document)), "Second parameter should be Document");
        Assert.That(parameters[2].ParameterType, Is.EqualTo(typeof(AmountMatchingConfig)), "Third parameter should be AmountMatchingConfig");
    }

    [Test]
    public void DeprecatedMethod_StillWorksForBackwardCompatibility()
    {
        // Arrange - Test the deprecated GetSkontoAdjustedAmount method
        var documentWithoutSkonto = new Document { Total = 100.00m, Skonto = null };
        var documentWithZeroSkonto = new Document { Total = 100.00m, Skonto = 0.00m };

        // Act & Assert - Deprecated method should still work as expected
        #pragma warning disable CS0618 // Type or member is obsolete
        var result1 = AmountMatcher.GetSkontoAdjustedAmount(documentWithoutSkonto);
        var result2 = AmountMatcher.GetSkontoAdjustedAmount(documentWithZeroSkonto);
        #pragma warning restore CS0618 // Type or member is obsolete

        Assert.That(result1, Is.Null, "Document without Skonto should return null");
        Assert.That(result2, Is.Null, "Document with zero Skonto should return null");
    }
}