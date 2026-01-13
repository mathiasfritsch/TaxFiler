using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using NSubstitute;
using System.Diagnostics;
using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFiler.Service.Test;

/// <summary>
/// Performance tests to verify that Skonto-aware matching doesn't significantly impact system performance.
/// </summary>
[TestFixture]
public class SkontoIntegrationPerformanceTests
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

        // Create service with all dependencies
        _service = new DocumentMatchingService(
            _context,
            _config,
            amountMatcher,
            dateMatcher,
            vendorMatcher,
            referenceMatcher);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task DocumentMatchingService_WithLargeDatasetAndSkonto_PerformanceAcceptable()
    {
        // Arrange - Create a large dataset with mixed Skonto scenarios
        var account = new Account { Id = 1, Name = "Performance Test Account" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Create 100 documents with various Skonto percentages
        var documents = new List<Document>();
        for (int i = 1; i <= 100; i++)
        {
            var skontoPercentage = i % 3 == 0 ? (decimal?)(2.0m + (i % 5)) : null; // Mix of Skonto and non-Skonto
            documents.Add(new Document(
                $"Performance Doc {i}", 
                $"perf-{i:D3}", 
                false, 
                19.0m, 
                (100.00m + i) * 0.19m, 
                100.00m + i, 
                100.00m + i - ((100.00m + i) * 0.19m),
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), 
                $"PERF-{i:D3}", 
                true, 
                skontoPercentage, 
                $"Vendor {i % 10}")); // Group vendors to increase matching likelihood
        }

        // Create 10 transactions that should match various documents
        var transactions = new List<Transaction>();
        for (int i = 1; i <= 10; i++)
        {
            var baseAmount = 100.00m + (i * 10);
            var skontoAmount = baseAmount * 0.98m; // Simulate 2% Skonto
            
            transactions.Add(new Transaction
            {
                Id = i,
                GrossAmount = i % 2 == 0 ? skontoAmount : baseAmount, // Mix of Skonto and full payments
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = $"Vendor {i % 10}",
                TransactionReference = $"PERF-{i * 10:D3}",
                TransactionNote = $"Performance test transaction {i}",
                SenderReceiver = $"Vendor {i % 10}",
                Account = account
            });
        }

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();

        // Act - Measure performance of batch matching
        var stopwatch = Stopwatch.StartNew();
        var batchMatches = await _service.BatchDocumentMatchesAsync(transactions);
        stopwatch.Stop();

        // Assert
        Assert.That(batchMatches, Has.Count.EqualTo(10), "Should process all transactions");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000), 
            "Batch matching with Skonto calculations should complete within 2 seconds for 10 transactions against 100 documents");

        // Verify that matches were found
        var totalMatches = batchMatches.Values.SelectMany(m => m).Count();
        Assert.That(totalMatches, Is.GreaterThan(0), "Should find some matches in performance test");

        Console.WriteLine($"Performance Test Results:");
        Console.WriteLine($"- Processed {transactions.Count} transactions against {documents.Count} documents");
        Console.WriteLine($"- Total execution time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"- Average time per transaction: {stopwatch.ElapsedMilliseconds / transactions.Count}ms");
        Console.WriteLine($"- Total matches found: {totalMatches}");
    }

    [Test]
    public async Task AmountMatcher_SkontoCalculationPerformance_AcceptableForHighVolume()
    {
        // Arrange - Create documents with and without Skonto
        var documentsWithSkonto = new List<Document>();
        var documentsWithoutSkonto = new List<Document>();
        
        for (int i = 1; i <= 500; i++)
        {
            documentsWithSkonto.Add(new Document(
                $"Skonto Doc {i}", $"skonto-{i}", false, null, null, 100.00m + i, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), $"SK-{i}", true, 2.5m, $"Vendor {i}"));
                
            documentsWithoutSkonto.Add(new Document(
                $"Regular Doc {i}", $"regular-{i}", false, null, null, 100.00m + i, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), $"RG-{i}", true, null, $"Vendor {i}"));
        }

        var transaction = new Transaction
        {
            GrossAmount = 150.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Test Vendor",
            TransactionReference = "PERF-TEST",
            TransactionNote = "Performance test",
            SenderReceiver = "Test Vendor"
        };

        var amountMatcher = new AmountMatcher();
        var config = new MatchingConfiguration();

        // Act - Measure Skonto calculation performance
        var stopwatchSkonto = Stopwatch.StartNew();
        foreach (var doc in documentsWithSkonto)
        {
            amountMatcher.CalculateAmountScore(transaction, doc, config.AmountConfig);
        }
        stopwatchSkonto.Stop();

        // Measure regular calculation performance for comparison
        var stopwatchRegular = Stopwatch.StartNew();
        foreach (var doc in documentsWithoutSkonto)
        {
            amountMatcher.CalculateAmountScore(transaction, doc, config.AmountConfig);
        }
        stopwatchRegular.Stop();

        // Assert
        Assert.That(stopwatchSkonto.ElapsedMilliseconds, Is.LessThan(1000), 
            "Skonto calculations should complete within 1 second for 500 documents");
        
        // Skonto calculations should not be significantly slower than regular calculations
        // Handle case where regular calculations are too fast to measure accurately
        if (stopwatchRegular.ElapsedMilliseconds > 0)
        {
            var performanceRatio = (double)stopwatchSkonto.ElapsedMilliseconds / stopwatchRegular.ElapsedMilliseconds;
            Assert.That(performanceRatio, Is.LessThan(5.0), 
                "Skonto calculations should not be more than 5x slower than regular calculations");
            
            Console.WriteLine($"Amount Matching Performance Results:");
            Console.WriteLine($"- Skonto calculations (500 docs): {stopwatchSkonto.ElapsedMilliseconds}ms");
            Console.WriteLine($"- Regular calculations (500 docs): {stopwatchRegular.ElapsedMilliseconds}ms");
            Console.WriteLine($"- Performance ratio: {performanceRatio:F2}x");
        }
        else
        {
            // Both calculations are very fast, which is good
            Console.WriteLine($"Amount Matching Performance Results:");
            Console.WriteLine($"- Skonto calculations (500 docs): {stopwatchSkonto.ElapsedMilliseconds}ms");
            Console.WriteLine($"- Regular calculations (500 docs): {stopwatchRegular.ElapsedMilliseconds}ms");
            Console.WriteLine($"- Both calculations are extremely fast (< 1ms)");
        }
    }

    [Test]
    public async Task DocumentMatchingService_RealisticGermanBusinessScenario_PerformanceAndAccuracy()
    {
        // Arrange - Create realistic German business scenario
        var account = new Account { Id = 1, Name = "Geschäftskonto" };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Create realistic German business documents
        var documents = new[]
        {
            // Office supplies with 2% Skonto
            new Document("Büromaterial März", "bm-2024-001", false, 19.0m, 38.02m, 238.02m, 200.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 10)), "RE-2024-001234", true, 2.0m, "Büro & Mehr GmbH"),
            
            // IT equipment with 3% Skonto
            new Document("Laptop Dell", "it-2024-002", false, 19.0m, 285.00m, 1785.00m, 1500.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 12)), "IT-2024-005678", true, 3.0m, "TechnoMax AG"),
            
            // Consulting without Skonto
            new Document("Steuerberatung Q1", "sb-2024-003", false, 19.0m, 95.00m, 595.00m, 500.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 14)), "SB-2024-009876", true, null, "Steuerberater Schmidt"),
            
            // Utilities with 1% Skonto
            new Document("Stromrechnung März", "sr-2024-004", false, 19.0m, 47.50m, 297.50m, 250.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "SR-2024-111222", true, 1.0m, "Stadtwerke München"),
        };

        // Create corresponding transactions with realistic German payment behavior
        var transactions = new[]
        {
            // Payment with 2% Skonto applied (238.02 * 0.98 = 233.26)
            new Transaction
            {
                Id = 1, GrossAmount = 233.26m,
                TransactionDateTime = new DateTime(2024, 3, 20),
                Counterparty = "BÜRO & MEHR GMBH", TransactionReference = "RE-2024-001234",
                TransactionNote = "Zahlung mit 2% Skonto", SenderReceiver = "BÜRO & MEHR",
                Account = account
            },
            
            // Payment with 3% Skonto applied (1785.00 * 0.97 = 1731.45)
            new Transaction
            {
                Id = 2, GrossAmount = 1731.45m,
                TransactionDateTime = new DateTime(2024, 3, 22),
                Counterparty = "TECHNOMAX AG", TransactionReference = "IT-2024-005678",
                TransactionNote = "Zahlung mit 3% Skonto", SenderReceiver = "TECHNOMAX",
                Account = account
            },
            
            // Full payment without Skonto
            new Transaction
            {
                Id = 3, GrossAmount = 595.00m,
                TransactionDateTime = new DateTime(2024, 3, 25),
                Counterparty = "STEUERBERATER SCHMIDT", TransactionReference = "SB-2024-009876",
                TransactionNote = "Vollzahlung Beratung", SenderReceiver = "SCHMIDT",
                Account = account
            }
        };

        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();

        // Act - Measure performance and accuracy
        var stopwatch = Stopwatch.StartNew();
        var allMatches = new List<DocumentMatch>();
        
        foreach (var transaction in transactions)
        {
            var matches = await _service.DocumentMatchesAsync(transaction);
            allMatches.AddRange(matches);
        }
        stopwatch.Stop();

        // Assert - Performance
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500), 
            "Realistic German business scenario should complete quickly");

        // Assert - Accuracy for Skonto transactions
        var skontoTransactions = transactions.Take(2).ToArray(); // First two have Skonto
        foreach (var transaction in skontoTransactions)
        {
            var matches = await _service.DocumentMatchesAsync(transaction);
            var bestMatch = matches.FirstOrDefault();
            
            Assert.That(bestMatch, Is.Not.Null, $"Should find match for transaction {transaction.Id}");
            Assert.That(bestMatch.ScoreBreakdown.AmountScore, Is.EqualTo(1.0), 
                $"Transaction {transaction.Id} should have perfect amount match with Skonto");
            Assert.That(bestMatch.MatchScore, Is.GreaterThan(0.8), 
                $"Transaction {transaction.Id} should have high overall match score");
        }

        Console.WriteLine($"German Business Scenario Results:");
        Console.WriteLine($"- Processed {transactions.Length} transactions against {documents.Length} documents");
        Console.WriteLine($"- Total execution time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"- Found {allMatches.Count} total matches");
        Console.WriteLine($"- All Skonto transactions matched correctly with perfect amount scores");
    }
}