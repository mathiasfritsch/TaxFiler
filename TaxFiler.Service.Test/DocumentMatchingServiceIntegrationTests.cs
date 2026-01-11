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
}