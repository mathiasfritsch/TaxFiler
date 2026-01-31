using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFiler.Service.Test;

/// <summary>
/// Test-specific TaxFilerContext that uses in-memory database
/// </summary>
public class TestTaxFilerContext : TaxFilerContext
{
    public TestTaxFilerContext() : base(CreateMockConfiguration())
    {
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var config = Substitute.For<IConfiguration>();
        config.GetConnectionString("TaxFilerNeonDB").Returns((string?)null);
        return config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
    }
}

/// <summary>
/// Integration tests for DocumentMatchingService to verify end-to-end functionality
/// with realistic German tax document data.
/// </summary>
[TestFixture]
public class DocumentMatchingServiceIntegrationTests
{
    private TestTaxFilerContext _context;
    private DocumentMatchingService _service;
    private MatchingConfiguration _config;

    [SetUp]
    public void Setup()
    {
        // Create test context with in-memory database
        _context = new TestTaxFilerContext();

        // Create default configuration
        _config = new MatchingConfiguration();

        // Create individual matchers
        var amountMatcher = new AmountMatcher();
        var dateMatcher = new DateMatcher();
        var vendorMatcher = new VendorMatcher();
        var referenceMatcher = new ReferenceMatcher();

        // Create attachment service and logger
        var attachmentLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentAttachmentService>();
        var attachmentService = new DocumentAttachmentService(_context, attachmentLogger);
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentMatchingService>();

        // Create service with all dependencies
        _service = new DocumentMatchingService(
            _context,
            _config,
            amountMatcher,
            dateMatcher,
            vendorMatcher,
            referenceMatcher,
            attachmentService,
            logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task DocumentMatchesAsync_WithRealisticGermanData_ReturnsRankedMatches()
    {
        // Arrange - Create realistic German tax documents and transactions
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        var documents = new[]
        {
            new Document("REWE Receipt", "rewe-001", false, null, null, 45.67m, null, 
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "RE-2024-001234", true, null, "REWE Markt GmbH"),
            new Document("Müller Receipt", "mueller-001", false, null, null, 23.45m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "MUE-456789", true, null, "Müller Drogeriemarkt")
        };

        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 45.67m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "REWE MARKT",
            TransactionReference = "RE-2024-001234",
            TransactionNote = "Grocery shopping",
            SenderReceiver = "REWE",
            Account = account
        };

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        // Act
        var matches = await _service.DocumentMatchesAsync(transaction);
        var matchList = matches.ToList();

        // Assert
        Assert.That(matchList, Is.Not.Empty, "Should return matches for realistic data");
        Assert.That(matchList.First().Document.VendorName, Is.EqualTo("REWE Markt GmbH"), "Best match should be REWE document");
        Assert.That(matchList.First().MatchScore, Is.GreaterThan(0.8), "Best match should have high confidence");
    }

    [Test]
    public async Task DocumentMatchesAsync_WithNullTransaction_ReturnsEmptyList()
    {
        // Act
        var matches = await _service.DocumentMatchesAsync((Transaction)null);

        // Assert
        Assert.That(matches, Is.Empty, "Should return empty list for null transaction");
    }

    [Test]
    public async Task DocumentMatchesAsync_WithSkontoDocument_MatchesDiscountedAmount()
    {
        // Arrange - Create document with Skonto and transaction with discounted amount
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Document with 2% Skonto: 100.00 EUR with 2% discount = 98.00 EUR expected payment
        var documentWithSkonto = new Document(
            "Invoice with Skonto", 
            "skonto-001", 
            false, 
            null, 
            null, 
            100.00m, // Total amount
            null,
            DateOnly.FromDateTime(new DateTime(2024, 3, 15)), 
            "INV-2024-001", 
            true, 
            2.0m, // 2% Skonto
            "Test Vendor GmbH");

        // Transaction with the discounted amount (after Skonto applied)
        var transactionWithDiscount = new Transaction
        {
            Id = 1,
            GrossAmount = 98.00m, // Amount after 2% Skonto discount
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Test Vendor",
            TransactionReference = "INV-2024-001",
            TransactionNote = "Payment with early discount",
            SenderReceiver = "Test Vendor",
            Account = account
        };

        await _context.Documents.AddAsync(documentWithSkonto);
        await _context.Transactions.AddAsync(transactionWithDiscount);
        await _context.SaveChangesAsync();

        // Act
        var matches = await _service.DocumentMatchesAsync(transactionWithDiscount);
        var matchList = matches.ToList();

        // Assert
        Assert.That(matchList, Is.Not.Empty, "Should find matches for Skonto-adjusted amounts");
        var bestMatch = matchList.First();
        Assert.That(bestMatch.Document.Id, Is.EqualTo(documentWithSkonto.Id), "Should match the Skonto document");
        Assert.That(bestMatch.MatchScore, Is.GreaterThan(0.8), "Should have high confidence match for Skonto-adjusted amount");
        Assert.That(bestMatch.ScoreBreakdown.AmountScore, Is.EqualTo(1.0), "Amount score should be perfect for exact Skonto match");
    }

    [Test]
    public async Task DocumentMatchesAsync_WithMultipleSkontoDocuments_RanksCorrectly()
    {
        // Arrange - Create multiple documents with different Skonto percentages
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        var documents = new[]
        {
            // Document 1: 100 EUR with 2% Skonto = 98 EUR expected
            new Document("Invoice 2% Skonto", "skonto-2pct", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, 2.0m, "Vendor A"),
            
            // Document 2: 100 EUR with 3% Skonto = 97 EUR expected  
            new Document("Invoice 3% Skonto", "skonto-3pct", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-002", true, 3.0m, "Vendor B"),
            
            // Document 3: 100 EUR with no Skonto = 100 EUR expected
            new Document("Invoice No Skonto", "no-skonto", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-003", true, null, "Vendor C")
        };

        // Transaction that matches the 2% Skonto document exactly
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 98.00m, // Matches 2% Skonto discount
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "Payment with 2% discount",
            SenderReceiver = "Vendor A",
            Account = account
        };

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        // Act
        var matches = await _service.DocumentMatchesAsync(transaction);
        var matchList = matches.ToList();

        // Assert
        Assert.That(matchList, Is.Not.Empty, "Should find matches");
        
        // The 2% Skonto document should be the best match
        var bestMatch = matchList.First();
        Assert.That(bestMatch.Document.Skonto, Is.EqualTo(2.0m), "Best match should be the 2% Skonto document");
        Assert.That(bestMatch.ScoreBreakdown.AmountScore, Is.EqualTo(1.0), "Amount score should be perfect for exact Skonto match");
        
        // Verify that other documents have lower scores
        var otherMatches = matchList.Skip(1).ToList();
        foreach (var match in otherMatches)
        {
            Assert.That(match.MatchScore, Is.LessThan(bestMatch.MatchScore), 
                $"Document {match.Document.Name} should have lower score than best match");
        }
    }

    [Test]
    public async Task DocumentMatchesAsync_WithInvalidSkontoValues_HandlesGracefully()
    {
        // Arrange - Create documents with various invalid Skonto values
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        var documents = new[]
        {
            // Document with zero Skonto (should use full amount)
            new Document("Zero Skonto", "zero-skonto", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, 0.0m, "Vendor A"),
            
            // Document with negative Skonto (should use full amount)
            new Document("Negative Skonto", "neg-skonto", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-002", true, -5.0m, "Vendor B"),
            
            // Document with excessive Skonto (should be capped)
            new Document("Excessive Skonto", "excess-skonto", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-003", true, 150.0m, "Vendor C")
        };

        // Transaction that matches full amount (for zero and negative Skonto cases)
        var transactionFullAmount = new Transaction
        {
            Id = 1,
            GrossAmount = 100.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "Full amount payment",
            SenderReceiver = "Vendor A",
            Account = account
        };

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddAsync(transactionFullAmount);
        await _context.SaveChangesAsync();

        // Act
        var matches = await _service.DocumentMatchesAsync(transactionFullAmount);
        var matchList = matches.ToList();

        // Assert
        Assert.That(matchList, Is.Not.Empty, "Should find matches even with invalid Skonto values");
        
        // Zero and negative Skonto documents should match the full amount
        var zeroSkontoMatch = matchList.FirstOrDefault(m => m.Document.Name == "Zero Skonto");
        var negativeSkontoMatch = matchList.FirstOrDefault(m => m.Document.Name == "Negative Skonto");
        
        Assert.That(zeroSkontoMatch, Is.Not.Null, "Should find match for zero Skonto document");
        Assert.That(negativeSkontoMatch, Is.Not.Null, "Should find match for negative Skonto document");
        
        // Both should have high amount scores since they match the full amount
        Assert.That(zeroSkontoMatch.ScoreBreakdown.AmountScore, Is.EqualTo(1.0), 
            "Zero Skonto should match full amount perfectly");
        Assert.That(negativeSkontoMatch.ScoreBreakdown.AmountScore, Is.EqualTo(1.0), 
            "Negative Skonto should match full amount perfectly");
    }

    [Test]
    public async Task DocumentMatchesAsync_WithRealisticGermanSkontoData_MatchesCorrectly()
    {
        // Arrange - Create realistic German business scenario with Skonto terms
        var account = new Account { Id = 1, Name = "Geschäftskonto" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        var documents = new[]
        {
            // Typical German invoice with 2% Skonto within 14 days
            new Document("Büromaterial Rechnung", "bm-2024-001", false, 19.0m, 38.02m, 238.02m, 200.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 10)), "RE-2024-001234", true, 2.0m, "Büro & Mehr GmbH"),
            
            // Office supplies invoice with 3% Skonto within 10 days
            new Document("IT Equipment", "it-2024-002", false, 19.0m, 95.00m, 595.00m, 500.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 12)), "IT-2024-005678", true, 3.0m, "TechnoMax AG"),
            
            // Service invoice without Skonto
            new Document("Beratungsleistung", "bl-2024-003", false, 19.0m, 47.50m, 297.50m, 250.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 14)), "BL-2024-009876", true, null, "Consulting Pro GmbH")
        };

        // Transaction for the first document with 2% Skonto applied
        // Original: 238.02 EUR, with 2% Skonto: 238.02 - (238.02 * 0.02) = 233.26 EUR
        var skontoTransaction = new Transaction
        {
            Id = 1,
            GrossAmount = 233.26m, // Amount after 2% Skonto discount
            TransactionDateTime = new DateTime(2024, 3, 20), // Within Skonto period
            Counterparty = "BÜRO & MEHR GMBH",
            TransactionReference = "RE-2024-001234",
            TransactionNote = "Zahlung mit Skonto 2%",
            SenderReceiver = "BÜRO & MEHR",
            Account = account
        };

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddAsync(skontoTransaction);
        await _context.SaveChangesAsync();

        // Act
        var matches = await _service.DocumentMatchesAsync(skontoTransaction);
        var matchList = matches.ToList();

        // Assert
        Assert.That(matchList, Is.Not.Empty, "Should find matches for realistic German Skonto scenario");
        
        var bestMatch = matchList.First();
        Assert.That(bestMatch.Document.VendorName, Is.EqualTo("Büro & Mehr GmbH"), 
            "Should match the correct German vendor");
        Assert.That(bestMatch.Document.Skonto, Is.EqualTo(2.0m), 
            "Should match the document with 2% Skonto");
        Assert.That(bestMatch.ScoreBreakdown.AmountScore, Is.EqualTo(1.0), 
            "Amount should match perfectly with Skonto calculation");
        Assert.That(bestMatch.MatchScore, Is.GreaterThan(0.8), 
            "Overall match score should be high for realistic scenario");
    }

    [Test]
    public async Task BatchDocumentMatchesAsync_WithSkontoDocuments_ProcessesAllCorrectly()
    {
        // Arrange - Create multiple transactions and documents with Skonto
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        var documents = new[]
        {
            new Document("Doc 1", "doc1", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, 2.0m, "Vendor A"),
            new Document("Doc 2", "doc2", false, null, null, 200.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "INV-002", true, 3.0m, "Vendor B")
        };

        var transactions = new[]
        {
            new Transaction
            {
                Id = 1, GrossAmount = 98.00m, // 2% Skonto applied
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A", TransactionReference = "INV-001",
                TransactionNote = "Payment with 2% Skonto",
                SenderReceiver = "Vendor A",
                Account = account
            },
            new Transaction
            {
                Id = 2, GrossAmount = 194.00m, // 3% Skonto applied  
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Vendor B", TransactionReference = "INV-002",
                TransactionNote = "Payment with 3% Skonto",
                SenderReceiver = "Vendor B",
                Account = account
            }
        };

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();

        // Act
        var batchMatches = await _service.BatchDocumentMatchesAsync(transactions);

        // Assert
        Assert.That(batchMatches, Has.Count.EqualTo(2), "Should process both transactions");
        
        foreach (var transactionId in batchMatches.Keys)
        {
            var matches = batchMatches[transactionId].ToList();
            Assert.That(matches, Is.Not.Empty, $"Transaction {transactionId} should have matches");
            
            var bestMatch = matches.First();
            Assert.That(bestMatch.ScoreBreakdown.AmountScore, Is.EqualTo(1.0), 
                $"Transaction {transactionId} should have perfect amount match with Skonto");
        }
    }

    [Test]
    public async Task DocumentMatchesAsync_PerformanceWithSkontoCalculations_RemainsAcceptable()
    {
        // Arrange - Create a larger dataset to test performance impact
        var account = new Account { Id = 1, Name = "Performance Test Account" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Create 50 documents with various Skonto percentages
        var documents = new List<Document>();
        for (int i = 1; i <= 50; i++)
        {
            var skontoPercentage = i % 5 == 0 ? (decimal?)(i % 10) : null; // Some with Skonto, some without
            documents.Add(new Document(
                $"Performance Doc {i}", 
                $"perf-{i:D3}", 
                false, 
                null, 
                null, 
                100.00m + i, 
                null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), 
                $"PERF-{i:D3}", 
                true, 
                skontoPercentage, 
                $"Vendor {i}"));
        }

        var testTransaction = new Transaction
        {
            Id = 1,
            GrossAmount = 95.00m, // Should match documents with ~5% Skonto
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Test Vendor",
            TransactionReference = "PERF-TEST",
            TransactionNote = "Performance test transaction",
            SenderReceiver = "Test Vendor",
            Account = account
        };

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddAsync(testTransaction);
        await _context.SaveChangesAsync();

        // Act - Measure performance
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var matches = await _service.DocumentMatchesAsync(testTransaction);
        stopwatch.Stop();

        // Assert
        var matchList = matches.ToList();
        Assert.That(matchList, Is.Not.Empty, "Should find matches in performance test");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), 
            "Skonto calculations should not significantly impact performance (< 1 second for 50 documents)");
        
        // Verify that Skonto calculations are working correctly
        var skontoMatches = matchList.Where(m => m.Document.Skonto.HasValue).ToList();
        Assert.That(skontoMatches, Is.Not.Empty, "Should find some matches with Skonto documents");
    }
}