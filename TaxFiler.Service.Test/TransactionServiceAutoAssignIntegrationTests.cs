using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFiler.Service.Test;

/// <summary>
/// Integration tests for TransactionService.AutoAssignDocumentsAsync method.
/// Tests end-to-end auto-assignment flow with real matching service.
/// Tests Requirements: 1.1, 1.2, 1.5, 2.2, 4.1, 4.2, 4.3, 5.1
/// </summary>
[TestFixture]
public class TransactionServiceAutoAssignIntegrationTests
{
    private TestTaxFilerContext _context;
    private DocumentMatchingService _matchingService;
    private TransactionService _service;
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
        
        // Create real matching service
        _matchingService = new DocumentMatchingService(
            _context,
            _config,
            amountMatcher,
            dateMatcher,
            vendorMatcher,
            referenceMatcher);
        
        // Create service with real dependencies
        _service = new TransactionService(_context, _matchingService);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Test: End-to-end auto-assignment flow with perfect matches
    /// Validates: Requirements 1.1, 1.2, 1.5, 2.2, 4.1, 4.2, 4.3
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_EndToEnd_AssignsDocumentsCorrectly()
    {
        // Arrange - Create realistic test data
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        // Create documents that should match transactions
        var documents = new[]
        {
            new Document("REWE Receipt", "rewe-001", false, null, null, 45.67m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "RE-2024-001234", true, null, "REWE Markt GmbH"),
            new Document("Müller Receipt", "mueller-001", false, null, null, 23.45m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "MUE-456789", true, null, "Müller Drogeriemarkt"),
            new Document("Office Supplies", "office-001", false, null, null, 150.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 17)), "OFF-2024-999", true, null, "Büro & Mehr GmbH")
        };
        
        await _context.Documents.AddRangeAsync(documents);
        await _context.SaveChangesAsync(); // Save documents first to get IDs
        
        // Create transactions - mix of unmatched and already matched
        var transactions = new[]
        {
            // Unmatched transaction 1 - should match REWE document
            new Transaction
            {
                Id = 1,
                GrossAmount = 45.67m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "REWE MARKT",
                TransactionReference = "RE-2024-001234",
                TransactionNote = "Grocery shopping",
                SenderReceiver = "REWE",
                Account = account,
                DocumentId = null // Unmatched
            },
            // Unmatched transaction 2 - should match Müller document
            new Transaction
            {
                Id = 2,
                GrossAmount = 23.45m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "MÜLLER DROGERIEMARKT",
                TransactionReference = "MUE-456789",
                TransactionNote = "Drugstore purchase",
                SenderReceiver = "MÜLLER",
                Account = account,
                DocumentId = null // Unmatched
            },
            // Already matched transaction - should be skipped
            new Transaction
            {
                Id = 3,
                GrossAmount = 150.00m,
                TransactionDateTime = new DateTime(2024, 3, 17),
                Counterparty = "BÜRO & MEHR",
                TransactionReference = "OFF-2024-999",
                TransactionNote = "Office supplies",
                SenderReceiver = "BÜRO & MEHR",
                Account = account,
                DocumentId = documents[2].Id // Already matched
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert - Verify results summary
        Assert.That(result.TotalProcessed, Is.EqualTo(2), "Should process 2 unmatched transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(2), "Should assign 2 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions (all have good matches)");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify documents are assigned correctly
        var transaction1 = await _context.Transactions.FindAsync(1);
        var transaction2 = await _context.Transactions.FindAsync(2);
        var transaction3 = await _context.Transactions.FindAsync(3);
        
        Assert.That(transaction1!.DocumentId, Is.EqualTo(documents[0].Id), 
            "Transaction 1 should be assigned to REWE document");
        Assert.That(transaction2!.DocumentId, Is.EqualTo(documents[1].Id), 
            "Transaction 2 should be assigned to Müller document");
        Assert.That(transaction3!.DocumentId, Is.EqualTo(documents[2].Id), 
            "Transaction 3 should remain assigned to Office document");
    }

    /// <summary>
    /// Test: Auto-assignment with matches below threshold
    /// Validates: Requirements 2.2 - Should skip transactions with low match scores
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithMatchesBelowThreshold_SkipsTransactions()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        // Create document that won't match well
        var document = new Document("Unrelated Document", "unrel-001", false, null, null, 999.99m, null,
            DateOnly.FromDateTime(new DateTime(2024, 3, 1)), "UNREL-001", true, null, "Unrelated Vendor");
        
        // Create transaction that won't match well (different amount, date, vendor, reference)
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 45.67m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "REWE MARKT",
            TransactionReference = "RE-2024-001234",
            TransactionNote = "Grocery shopping",
            SenderReceiver = "REWE",
            Account = account,
            DocumentId = null
        };
        
        await _context.Documents.AddAsync(document);
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(1), "Should process 1 transaction");
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents (below threshold)");
        Assert.That(result.SkippedCount, Is.EqualTo(1), "Should skip 1 transaction");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify transaction remains unassigned
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction!.DocumentId, Is.Null, "Transaction should remain unassigned");
    }

    /// <summary>
    /// Test: Auto-assignment with no available documents
    /// Validates: Requirements 5.1 - Should handle empty document set gracefully
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithNoAvailableDocuments_SkipsAllTransactions()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        // Create transactions but no documents
        var transactions = new[]
        {
            new Transaction
            {
                Id = 1,
                GrossAmount = 45.67m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "REWE MARKT",
                TransactionReference = "RE-2024-001234",
                TransactionNote = "Grocery shopping",
                SenderReceiver = "REWE",
                Account = account,
                DocumentId = null
            },
            new Transaction
            {
                Id = 2,
                GrossAmount = 23.45m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "MÜLLER",
                TransactionReference = "MUE-456789",
                TransactionNote = "Drugstore",
                SenderReceiver = "MÜLLER",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(2), "Should process 2 transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents (no documents available)");
        Assert.That(result.SkippedCount, Is.EqualTo(2), "Should skip 2 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify transactions remain unassigned
        var transaction1 = await _context.Transactions.FindAsync(1);
        var transaction2 = await _context.Transactions.FindAsync(2);
        Assert.That(transaction1!.DocumentId, Is.Null, "Transaction 1 should remain unassigned");
        Assert.That(transaction2!.DocumentId, Is.Null, "Transaction 2 should remain unassigned");
    }

    /// <summary>
    /// Test: Auto-assignment with Skonto documents
    /// Validates: Requirements 1.2, 2.2 - Should match Skonto-adjusted amounts correctly
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithSkontoDocuments_MatchesCorrectly()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        // Create documents with Skonto
        var documents = new[]
        {
            // Document with 2% Skonto: 100.00 EUR with 2% discount = 98.00 EUR expected payment
            new Document("Invoice with 2% Skonto", "skonto-001", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-2024-001", true, 2.0m, "Vendor A"),
            // Document with 3% Skonto: 200.00 EUR with 3% discount = 194.00 EUR expected payment
            new Document("Invoice with 3% Skonto", "skonto-002", false, null, null, 200.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "INV-2024-002", true, 3.0m, "Vendor B")
        };
        
        // Create transactions with Skonto-adjusted amounts
        var transactions = new[]
        {
            new Transaction
            {
                Id = 1,
                GrossAmount = 98.00m, // 2% Skonto applied
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A",
                TransactionReference = "INV-2024-001",
                TransactionNote = "Payment with 2% discount",
                SenderReceiver = "Vendor A",
                Account = account,
                DocumentId = null
            },
            new Transaction
            {
                Id = 2,
                GrossAmount = 194.00m, // 3% Skonto applied
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Vendor B",
                TransactionReference = "INV-2024-002",
                TransactionNote = "Payment with 3% discount",
                SenderReceiver = "Vendor B",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(2), "Should process 2 transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(2), "Should assign 2 documents with Skonto");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify correct Skonto documents are assigned
        var transaction1 = await _context.Transactions.FindAsync(1);
        var transaction2 = await _context.Transactions.FindAsync(2);
        
        Assert.That(transaction1!.DocumentId, Is.EqualTo(documents[0].Id), 
            "Transaction 1 should be assigned to 2% Skonto document");
        Assert.That(transaction2!.DocumentId, Is.EqualTo(documents[1].Id), 
            "Transaction 2 should be assigned to 3% Skonto document");
    }

    /// <summary>
    /// Test: Auto-assignment with multiple matching documents
    /// Validates: Requirements 2.2 - Should select document with highest match score
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithMultipleMatches_SelectsBestMatch()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        // Create multiple documents that could match the transaction
        var documents = new[]
        {
            // Perfect match - all fields match
            new Document("Perfect Match", "perfect-001", false, null, null, 45.67m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "RE-2024-001234", true, null, "REWE Markt GmbH"),
            // Good match - amount and date match, but vendor slightly different
            new Document("Good Match", "good-001", false, null, null, 45.67m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "RE-2024-999", true, null, "REWE Store"),
            // Weak match - only amount matches
            new Document("Weak Match", "weak-001", false, null, null, 45.67m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 1)), "WEAK-001", true, null, "Different Vendor")
        };
        
        // Create transaction that matches all documents to varying degrees
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 45.67m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "REWE MARKT",
            TransactionReference = "RE-2024-001234",
            TransactionNote = "Grocery shopping",
            SenderReceiver = "REWE",
            Account = account,
            DocumentId = null
        };
        
        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(1), "Should process 1 transaction");
        Assert.That(result.AssignedCount, Is.EqualTo(1), "Should assign 1 document");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify the best match (perfect match) is assigned
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction!.DocumentId, Is.EqualTo(documents[0].Id), 
            "Should assign the perfect match document");
    }

    /// <summary>
    /// Test: Auto-assignment with realistic German business data
    /// Validates: Requirements 1.1, 1.2, 2.2, 4.1, 4.2, 4.3
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithRealisticGermanData_AssignsCorrectly()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Geschäftskonto" };
        await _context.Accounts.AddAsync(account);
        
        // Create realistic German business documents
        var documents = new[]
        {
            new Document("Büromaterial Rechnung", "bm-2024-001", false, 19.0m, 38.02m, 238.02m, 200.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 10)), "RE-2024-001234", true, 2.0m, "Büro & Mehr GmbH"),
            new Document("IT Equipment", "it-2024-002", false, 19.0m, 95.00m, 595.00m, 500.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 12)), "IT-2024-005678", true, 3.0m, "TechnoMax AG"),
            new Document("Beratungsleistung", "bl-2024-003", false, 19.0m, 47.50m, 297.50m, 250.00m,
                DateOnly.FromDateTime(new DateTime(2024, 3, 14)), "BL-2024-009876", true, null, "Consulting Pro GmbH")
        };
        
        // Create realistic German transactions
        var transactions = new[]
        {
            // Transaction with 2% Skonto applied
            new Transaction
            {
                Id = 1,
                GrossAmount = 233.26m, // 238.02 - (238.02 * 0.02)
                TransactionDateTime = new DateTime(2024, 3, 20),
                Counterparty = "BÜRO & MEHR GMBH",
                TransactionReference = "RE-2024-001234",
                TransactionNote = "Zahlung mit Skonto 2%",
                SenderReceiver = "BÜRO & MEHR",
                Account = account,
                DocumentId = null
            },
            // Transaction with 3% Skonto applied
            new Transaction
            {
                Id = 2,
                GrossAmount = 577.15m, // 595.00 - (595.00 * 0.03)
                TransactionDateTime = new DateTime(2024, 3, 22),
                Counterparty = "TECHNOMAX AG",
                TransactionReference = "IT-2024-005678",
                TransactionNote = "IT-Ausstattung mit Skonto",
                SenderReceiver = "TECHNOMAX",
                Account = account,
                DocumentId = null
            },
            // Transaction without Skonto
            new Transaction
            {
                Id = 3,
                GrossAmount = 297.50m,
                TransactionDateTime = new DateTime(2024, 3, 25),
                Counterparty = "CONSULTING PRO GMBH",
                TransactionReference = "BL-2024-009876",
                TransactionNote = "Beratungsleistung",
                SenderReceiver = "CONSULTING PRO",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(3), "Should process 3 transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(3), "Should assign 3 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify correct assignments
        var transaction1 = await _context.Transactions.FindAsync(1);
        var transaction2 = await _context.Transactions.FindAsync(2);
        var transaction3 = await _context.Transactions.FindAsync(3);
        
        Assert.That(transaction1!.DocumentId, Is.EqualTo(documents[0].Id), 
            "Transaction 1 should be assigned to Büromaterial document");
        Assert.That(transaction2!.DocumentId, Is.EqualTo(documents[1].Id), 
            "Transaction 2 should be assigned to IT Equipment document");
        Assert.That(transaction3!.DocumentId, Is.EqualTo(documents[2].Id), 
            "Transaction 3 should be assigned to Beratungsleistung document");
    }

    /// <summary>
    /// Test: Auto-assignment with large batch of transactions
    /// Validates: Requirements 3.1, 3.2 - Should process batch efficiently
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithLargeBatch_ProcessesEfficiently()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        // Create 20 documents
        var documents = new List<Document>();
        for (int i = 1; i <= 20; i++)
        {
            documents.Add(new Document(
                $"Document {i}",
                $"doc-{i:D3}",
                false,
                null,
                null,
                100.00m + i,
                null,
                DateOnly.FromDateTime(new DateTime(2024, 3, i)),
                $"INV-{i:D3}",
                true,
                null,
                $"Vendor {i}"));
        }
        
        // Create 20 matching transactions
        var transactions = new List<Transaction>();
        for (int i = 1; i <= 20; i++)
        {
            transactions.Add(new Transaction
            {
                Id = i,
                GrossAmount = 100.00m + i,
                TransactionDateTime = new DateTime(2024, 3, i),
                Counterparty = $"Vendor {i}",
                TransactionReference = $"INV-{i:D3}",
                TransactionNote = $"Transaction {i}",
                SenderReceiver = $"Vendor {i}",
                Account = account,
                DocumentId = null
            });
        }
        
        await _context.Documents.AddRangeAsync(documents);
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        stopwatch.Stop();
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(20), "Should process 20 transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(20), "Should assign 20 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), 
            "Should process 20 transactions in less than 5 seconds");
        
        // Verify all transactions are assigned
        for (int i = 1; i <= 20; i++)
        {
            var transaction = await _context.Transactions.FindAsync(i);
            Assert.That(transaction!.DocumentId, Is.Not.Null, 
                $"Transaction {i} should be assigned");
        }
    }

    /// <summary>
    /// Test: Auto-assignment with operation cancellation (simulates network interruption)
    /// Validates: Requirements 5.1 - Should handle cancellation gracefully
    /// </summary>
    [Test]
    public void AutoAssignDocumentsAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        _context.Accounts.Add(account);
        
        var document = new Document("Test Document", "doc-001", false, null, null, 100.00m, null,
            DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Test Vendor");
        _context.Documents.Add(document);
        
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 100.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Test Vendor",
            TransactionReference = "INV-001",
            TransactionNote = "Test transaction",
            SenderReceiver = "Test Vendor",
            Account = account,
            DocumentId = null
        };
        _context.Transactions.Add(transaction);
        _context.SaveChanges();
        
        // Create a cancellation token that is already cancelled
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _service.AutoAssignDocumentsAsync(yearMonth, cts.Token);
        }, "Should throw OperationCanceledException when cancellation is requested");
    }

    /// <summary>
    /// Test: Auto-assignment with mixed success and failure scenarios
    /// Validates: Requirements 5.1 - Should handle partial failures gracefully
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithMixedScenarios_HandlesGracefully()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        // Create documents
        var documents = new[]
        {
            new Document("Good Match", "doc-001", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Vendor A"),
            new Document("Another Good Match", "doc-002", false, null, null, 200.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "INV-002", true, null, "Vendor B")
        };
        
        await _context.Documents.AddRangeAsync(documents);
        await _context.SaveChangesAsync();
        
        // Create transactions with various scenarios
        var transactions = new[]
        {
            // Transaction 1: Good match - should be assigned
            new Transaction
            {
                Id = 1,
                GrossAmount = 100.00m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A",
                TransactionReference = "INV-001",
                TransactionNote = "Good match",
                SenderReceiver = "Vendor A",
                Account = account,
                DocumentId = null
            },
            // Transaction 2: No match - should be skipped
            new Transaction
            {
                Id = 2,
                GrossAmount = 999.99m,
                TransactionDateTime = new DateTime(2024, 3, 20),
                Counterparty = "Unknown Vendor",
                TransactionReference = "UNKNOWN-999",
                TransactionNote = "No match",
                SenderReceiver = "Unknown",
                Account = account,
                DocumentId = null
            },
            // Transaction 3: Good match - should be assigned
            new Transaction
            {
                Id = 3,
                GrossAmount = 200.00m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Vendor B",
                TransactionReference = "INV-002",
                TransactionNote = "Another good match",
                SenderReceiver = "Vendor B",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(3), "Should process 3 transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(2), "Should assign 2 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(1), "Should skip 1 transaction (no match)");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify assignments
        var transaction1 = await _context.Transactions.FindAsync(1);
        var transaction2 = await _context.Transactions.FindAsync(2);
        var transaction3 = await _context.Transactions.FindAsync(3);
        
        Assert.That(transaction1!.DocumentId, Is.EqualTo(documents[0].Id), 
            "Transaction 1 should be assigned");
        Assert.That(transaction2!.DocumentId, Is.Null, 
            "Transaction 2 should remain unassigned (no match)");
        Assert.That(transaction3!.DocumentId, Is.EqualTo(documents[1].Id), 
            "Transaction 3 should be assigned");
    }
}
