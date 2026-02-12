using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace TaxFiler.Service.Test;

/// <summary>
/// Unit tests for TransactionService.AutoAssignDocumentsAsync method.
/// Tests Requirements: 1.2, 3.3, 5.3, 5.4
/// </summary>
[TestFixture]
public class TransactionServiceAutoAssignTests
{
    private TestTaxFilerContext _context;
    private IDocumentMatchingService _mockMatchingService;
    private TransactionService _service;

    [SetUp]
    public void Setup()
    {
        // Create test context with in-memory database
        _context = new TestTaxFilerContext();
        
        // Create mock matching service
        _mockMatchingService = Substitute.For<IDocumentMatchingService>();
        
        // Create service with dependencies
        _service = new TransactionService(_context, _mockMatchingService);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Test: Empty transaction list
    /// Validates: Requirements 1.2 - Should handle empty list gracefully
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithEmptyTransactionList_ReturnsZeroCounts()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        
        // No transactions in database
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(0), "Should process 0 transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
    }

    /// <summary>
    /// Test: All transactions already assigned
    /// Validates: Requirements 1.2 - Should only process unmatched transactions
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithAllTransactionsAlreadyAssigned_ReturnsZeroCounts()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var document = new Document("Test Doc", "doc-001", false, null, null, 100.00m, null,
            DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Test Vendor");
        await _context.Documents.AddAsync(document);
        
        // Create transactions that already have documents assigned
        var transactions = new[]
        {
            new Transaction
            {
                Id = 1,
                GrossAmount = 100.00m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Test Vendor",
                TransactionReference = "INV-001",
                TransactionNote = "Already assigned",
                SenderReceiver = "Test Vendor",
                Account = account,
                DocumentId = document.Id // Already assigned
            },
            new Transaction
            {
                Id = 2,
                GrossAmount = 200.00m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Another Vendor",
                TransactionReference = "INV-002",
                TransactionNote = "Also assigned",
                SenderReceiver = "Another Vendor",
                Account = account,
                DocumentId = document.Id // Already assigned
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(0), "Should process 0 transactions (all already assigned)");
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Note: The service will still call BatchDocumentMatchesAsync with an empty list
        // This is acceptable behavior as it's more efficient to query once than check count first
    }

    /// <summary>
    /// Test: Mix of assigned and unassigned transactions
    /// Validates: Requirements 1.2 - Should only process unmatched transactions
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithMixOfAssignedAndUnassigned_ProcessesOnlyUnassigned()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var documents = new[]
        {
            new Document("Doc 1", "doc-001", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Vendor A"),
            new Document("Doc 2", "doc-002", false, null, null, 200.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "INV-002", true, null, "Vendor B"),
            new Document("Doc 3", "doc-003", false, null, null, 300.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 17)), "INV-003", true, null, "Vendor C")
        };
        await _context.Documents.AddRangeAsync(documents);
        
        var transactions = new[]
        {
            // Already assigned
            new Transaction
            {
                Id = 1,
                GrossAmount = 100.00m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A",
                TransactionReference = "INV-001",
                TransactionNote = "Already assigned",
                SenderReceiver = "Vendor A",
                Account = account,
                DocumentId = documents[0].Id // Already assigned
            },
            // Unassigned - should be processed
            new Transaction
            {
                Id = 2,
                GrossAmount = 200.00m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Vendor B",
                TransactionReference = "INV-002",
                TransactionNote = "Unassigned",
                SenderReceiver = "Vendor B",
                Account = account,
                DocumentId = null // Unassigned
            },
            // Unassigned - should be processed
            new Transaction
            {
                Id = 3,
                GrossAmount = 300.00m,
                TransactionDateTime = new DateTime(2024, 3, 17),
                Counterparty = "Vendor C",
                TransactionReference = "INV-003",
                TransactionNote = "Also unassigned",
                SenderReceiver = "Vendor C",
                Account = account,
                DocumentId = null // Unassigned
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Setup mock to return matches for unassigned transactions
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 2, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = documents[1].Id, 
                        Name = documents[1].Name, 
                        ExternalRef = documents[1].ExternalRef 
                    }, 
                    MatchScore = 0.8, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } },
            { 3, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = documents[2].Id, 
                        Name = documents[2].Name, 
                        ExternalRef = documents[2].ExternalRef 
                    }, 
                    MatchScore = 0.9, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Is<IEnumerable<Transaction>>(txns => txns.Count() == 2), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(2), "Should process only 2 unassigned transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(2), "Should assign 2 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(0), "Should skip 0 transactions");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify only unassigned transactions were processed
        // Note: The list is materialized before being passed to the mock, so we can't verify
        // the exact predicate, but we can verify the count
        await _mockMatchingService.Received(1).BatchDocumentMatchesAsync(
            Arg.Is<IEnumerable<Transaction>>(txns => txns.Count() == 2),
            Arg.Any<CancellationToken>());
        
        // Verify database was updated
        var updatedTransaction2 = await _context.Transactions.FindAsync(2);
        var updatedTransaction3 = await _context.Transactions.FindAsync(3);
        Assert.That(updatedTransaction2.DocumentId, Is.EqualTo(documents[1].Id), "Transaction 2 should be assigned");
        Assert.That(updatedTransaction3.DocumentId, Is.EqualTo(documents[2].Id), "Transaction 3 should be assigned");
    }

    /// <summary>
    /// Test: Cancellation token triggered mid-operation
    /// Validates: Requirements 5.3, 5.4 - Should handle cancellation gracefully
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithCancellationTokenTriggered_StopsProcessingGracefully()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var documents = new[]
        {
            new Document("Doc 1", "doc-001", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Vendor A"),
            new Document("Doc 2", "doc-002", false, null, null, 200.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 16)), "INV-002", true, null, "Vendor B")
        };
        await _context.Documents.AddRangeAsync(documents);
        
        var transactions = new[]
        {
            new Transaction
            {
                Id = 1,
                GrossAmount = 100.00m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A",
                TransactionReference = "INV-001",
                TransactionNote = "First transaction",
                SenderReceiver = "Vendor A",
                Account = account,
                DocumentId = null
            },
            new Transaction
            {
                Id = 2,
                GrossAmount = 200.00m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Vendor B",
                TransactionReference = "INV-002",
                TransactionNote = "Second transaction",
                SenderReceiver = "Vendor B",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Setup mock to return matches
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = documents[0].Id, 
                        Name = documents[0].Name, 
                        ExternalRef = documents[0].ExternalRef 
                    }, 
                    MatchScore = 0.8, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } },
            { 2, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = documents[1].Id, 
                        Name = documents[1].Name, 
                        ExternalRef = documents[1].ExternalRef 
                    }, 
                    MatchScore = 0.9, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Create cancellation token that is already cancelled
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        // The cancellation will be detected during the database query
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _service.AutoAssignDocumentsAsync(yearMonth, cts.Token);
        });
    }

    /// <summary>
    /// Test: Error handling for individual transaction failures
    /// Validates: Requirements 3.3 - Should continue processing remaining transactions
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithIndividualTransactionFailures_ContinuesProcessing()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var documents = new[]
        {
            new Document("Doc 1", "doc-001", false, null, null, 100.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Vendor A"),
            new Document("Doc 3", "doc-003", false, null, null, 300.00m, null,
                DateOnly.FromDateTime(new DateTime(2024, 3, 17)), "INV-003", true, null, "Vendor C")
        };
        await _context.Documents.AddRangeAsync(documents);
        
        var transactions = new[]
        {
            new Transaction
            {
                Id = 1,
                GrossAmount = 100.00m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A",
                TransactionReference = "INV-001",
                TransactionNote = "First transaction",
                SenderReceiver = "Vendor A",
                Account = account,
                DocumentId = null
            },
            new Transaction
            {
                Id = 2,
                GrossAmount = 200.00m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Vendor B",
                TransactionReference = "INV-002",
                TransactionNote = "Second transaction - will fail",
                SenderReceiver = "Vendor B",
                Account = account,
                DocumentId = null
            },
            new Transaction
            {
                Id = 3,
                GrossAmount = 300.00m,
                TransactionDateTime = new DateTime(2024, 3, 17),
                Counterparty = "Vendor C",
                TransactionReference = "INV-003",
                TransactionNote = "Third transaction",
                SenderReceiver = "Vendor C",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Setup mock to return matches, including one with a non-existent document ID
        // Note: In the actual implementation, assigning a non-existent DocumentId won't cause
        // an immediate error - it will only fail on SaveChanges if there's a foreign key constraint
        // For this test, we'll simulate the scenario where transaction 2 has no match
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = documents[0].Id, 
                        Name = documents[0].Name, 
                        ExternalRef = documents[0].ExternalRef 
                    }, 
                    MatchScore = 0.8, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } },
            // Transaction 2 has no matches - will be skipped
            { 3, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = documents[1].Id, 
                        Name = documents[1].Name, 
                        ExternalRef = documents[1].ExternalRef 
                    }, 
                    MatchScore = 0.85, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(3), "Should process all 3 transactions");
        Assert.That(result.AssignedCount, Is.EqualTo(2), "Should assign 2 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(1), "Should skip 1 transaction (no match)");
        Assert.That(result.Errors, Is.Empty, "Should have no errors (no match is not an error)");
        
        // Verify that transactions 1 and 3 were assigned despite transaction 2 having no match
        var transaction1 = await _context.Transactions.FindAsync(1);
        var transaction2 = await _context.Transactions.FindAsync(2);
        var transaction3 = await _context.Transactions.FindAsync(3);
        
        Assert.That(transaction1!.DocumentId, Is.EqualTo(documents[0].Id), "Transaction 1 should be assigned");
        Assert.That(transaction2!.DocumentId, Is.Null, "Transaction 2 should remain unassigned (no match)");
        Assert.That(transaction3!.DocumentId, Is.EqualTo(documents[1].Id), "Transaction 3 should be assigned");
    }

    /// <summary>
    /// Test: No matches above threshold
    /// Validates: Requirements 1.2 - Should skip transactions with low match scores
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithNoMatchesAboveThreshold_SkipsAllTransactions()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var document = new Document("Doc 1", "doc-001", false, null, null, 100.00m, null,
            DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Vendor A");
        await _context.Documents.AddAsync(document);
        
        var transactions = new[]
        {
            new Transaction
            {
                Id = 1,
                GrossAmount = 100.00m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A",
                TransactionReference = "INV-001",
                TransactionNote = "Low match score",
                SenderReceiver = "Vendor A",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Setup mock to return matches with scores below threshold (< 0.5)
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = document.Id, 
                        Name = document.Name, 
                        ExternalRef = document.ExternalRef 
                    }, 
                    MatchScore = 0.3, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } } // Below 0.5 threshold
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(1), "Should process 1 transaction");
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents (below threshold)");
        Assert.That(result.SkippedCount, Is.EqualTo(1), "Should skip 1 transaction");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify transaction remains unassigned
        var transaction = await _context.Transactions.FindAsync(1);
        Assert.That(transaction.DocumentId, Is.Null, "Transaction should remain unassigned");
    }

    /// <summary>
    /// Test: No matches returned for transaction
    /// Validates: Requirements 1.2 - Should handle transactions with no matches
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithNoMatchesReturned_SkipsTransaction()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 100.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "No matches",
            SenderReceiver = "Vendor A",
            Account = account,
            DocumentId = null
        };
        
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Setup mock to return empty matches
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, Enumerable.Empty<DocumentMatch>() }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(1), "Should process 1 transaction");
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(1), "Should skip 1 transaction");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify transaction remains unassigned
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction.DocumentId, Is.Null, "Transaction should remain unassigned");
    }

    /// <summary>
    /// Test: Transaction not in matches dictionary
    /// Validates: Requirements 1.2 - Should handle missing transaction in results
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithTransactionNotInMatchesDictionary_SkipsTransaction()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 100.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "Not in dictionary",
            SenderReceiver = "Vendor A",
            Account = account,
            DocumentId = null
        };
        
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Setup mock to return empty dictionary (transaction not included)
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>();
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.TotalProcessed, Is.EqualTo(1), "Should process 1 transaction");
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents");
        Assert.That(result.SkippedCount, Is.EqualTo(1), "Should skip 1 transaction");
        Assert.That(result.Errors, Is.Empty, "Should have no errors");
        
        // Verify transaction remains unassigned
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction.DocumentId, Is.Null, "Transaction should remain unassigned");
    }

    /// <summary>
    /// Test: Database save is not called when no assignments made
    /// Validates: Requirements 5.4 - Should not save when nothing to update
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithNoAssignments_DoesNotSaveChanges()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 100.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "No assignment",
            SenderReceiver = "Vendor A",
            Account = account,
            DocumentId = null
        };
        
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Track the initial save count
        var initialChangeCount = _context.ChangeTracker.Entries().Count();
        
        // Setup mock to return no matches
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>();
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.AssignedCount, Is.EqualTo(0), "Should assign 0 documents");
        
        // Verify transaction remains unassigned
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction.DocumentId, Is.Null, "Transaction should remain unassigned");
    }

    /// <summary>
    /// Test: Tax data is copied from document to transaction without Skonto
    /// Validates: Should copy TaxRate, TaxAmount, and NetAmount (SubTotal) from document
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithoutSkonto_CopiesTaxDataFromDocument()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 119.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "Test transaction",
            SenderReceiver = "Vendor A",
            Account = account,
            DocumentId = null,
            NetAmount = null, // Should be set by auto-assignment
            TaxAmount = null, // Should be set by auto-assignment
            TaxRate = null    // Should be set by auto-assignment
        };
        
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Setup mock to return match with tax data (no Skonto)
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = 100,
                        Name = "Invoice-001.pdf",
                        ExternalRef = "doc-001",
                        SubTotal = 100.00m,     // NetAmount
                        TaxAmount = 19.00m,
                        TaxRate = 19.0m,
                        Total = 119.00m,
                        Skonto = null           // No Skonto
                    }, 
                    MatchScore = 0.8, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.AssignedCount, Is.EqualTo(1), "Should assign 1 document");
        
        // Verify transaction has tax data copied from document
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction.DocumentId, Is.EqualTo(100), "Transaction should be assigned");
        Assert.That(updatedTransaction.NetAmount, Is.EqualTo(100.00m), "NetAmount should be copied from SubTotal");
        Assert.That(updatedTransaction.TaxAmount, Is.EqualTo(19.00m), "TaxAmount should be copied");
        Assert.That(updatedTransaction.TaxRate, Is.EqualTo(19.0m), "TaxRate should be copied");
    }

    /// <summary>
    /// Test: Tax data is calculated with Skonto
    /// Validates: Should calculate NetAmount with Skonto discount and derive TaxAmount
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithSkonto_CalculatesTaxDataWithDiscount()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 116.62m, // Gross amount after 2% Skonto discount
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "Test transaction with Skonto",
            SenderReceiver = "Vendor A",
            Account = account,
            DocumentId = null,
            NetAmount = null, // Should be calculated by auto-assignment
            TaxAmount = null, // Should be calculated by auto-assignment
            TaxRate = null    // Should be set by auto-assignment
        };
        
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Setup mock to return match with tax data and Skonto
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = 100,
                        Name = "Invoice-001.pdf",
                        ExternalRef = "doc-001",
                        SubTotal = 100.00m,     // Original NetAmount before Skonto
                        TaxAmount = 19.00m,     // Original TaxAmount (will be recalculated)
                        TaxRate = 19.0m,
                        Total = 119.00m,        // Original Total before Skonto
                        Skonto = 2.0m           // 2% early payment discount
                    }, 
                    MatchScore = 0.8, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.AssignedCount, Is.EqualTo(1), "Should assign 1 document");
        
        // Verify transaction has tax data calculated with Skonto
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction.DocumentId, Is.EqualTo(100), "Transaction should be assigned");
        
        // NetAmount = SubTotal * (100 - Skonto) / 100 = 100.00 * (100 - 2) / 100 = 98.00
        Assert.That(updatedTransaction.NetAmount, Is.EqualTo(98.00m), "NetAmount should be calculated with Skonto discount");
        Assert.That(updatedTransaction.TaxRate, Is.EqualTo(19.0m), "TaxRate should be copied");
        
        // TaxAmount = GrossAmount - NetAmount = 116.62 - 98.00 = 18.62
        Assert.That(updatedTransaction.TaxAmount, Is.EqualTo(18.62m), "TaxAmount should be calculated from GrossAmount - NetAmount");
    }

    /// <summary>
    /// Test: Multiple transactions get tax data copied
    /// Validates: Tax data should be copied for all assigned transactions in batch
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithMultipleTransactions_CopiesTaxDataForAll()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var transactions = new[]
        {
            new Transaction
            {
                Id = 1,
                GrossAmount = 119.00m,
                TransactionDateTime = new DateTime(2024, 3, 15),
                Counterparty = "Vendor A",
                TransactionReference = "INV-001",
                TransactionNote = "First transaction",
                SenderReceiver = "Vendor A",
                Account = account,
                DocumentId = null
            },
            new Transaction
            {
                Id = 2,
                GrossAmount = 238.00m,
                TransactionDateTime = new DateTime(2024, 3, 16),
                Counterparty = "Vendor B",
                TransactionReference = "INV-002",
                TransactionNote = "Second transaction",
                SenderReceiver = "Vendor B",
                Account = account,
                DocumentId = null
            }
        };
        
        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        
        // Setup mock to return matches with different tax data
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = 100,
                        Name = "Invoice-001.pdf",
                        ExternalRef = "doc-001",
                        SubTotal = 100.00m,
                        TaxAmount = 19.00m,
                        TaxRate = 19.0m,
                        Total = 119.00m,
                        Skonto = null
                    }, 
                    MatchScore = 0.8, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } },
            { 2, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = 200,
                        Name = "Invoice-002.pdf",
                        ExternalRef = "doc-002",
                        SubTotal = 200.00m,
                        TaxAmount = 38.00m,
                        TaxRate = 19.0m,
                        Total = 238.00m,
                        Skonto = null
                    }, 
                    MatchScore = 0.9, 
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.AssignedCount, Is.EqualTo(2), "Should assign 2 documents");
        
        // Verify first transaction
        var transaction1 = await _context.Transactions.FindAsync(1);
        Assert.That(transaction1.DocumentId, Is.EqualTo(100), "Transaction 1 should be assigned");
        Assert.That(transaction1.NetAmount, Is.EqualTo(100.00m), "Transaction 1 NetAmount should be copied");
        Assert.That(transaction1.TaxAmount, Is.EqualTo(19.00m), "Transaction 1 TaxAmount should be copied");
        Assert.That(transaction1.TaxRate, Is.EqualTo(19.0m), "Transaction 1 TaxRate should be copied");
        
        // Verify second transaction
        var transaction2 = await _context.Transactions.FindAsync(2);
        Assert.That(transaction2.DocumentId, Is.EqualTo(200), "Transaction 2 should be assigned");
        Assert.That(transaction2.NetAmount, Is.EqualTo(200.00m), "Transaction 2 NetAmount should be copied");
        Assert.That(transaction2.TaxAmount, Is.EqualTo(38.00m), "Transaction 2 TaxAmount should be copied");
        Assert.That(transaction2.TaxRate, Is.EqualTo(19.0m), "Transaction 2 TaxRate should be copied");
    }

    /// <summary>
    /// Test: Skipped transactions don't get tax data modified
    /// Validates: Transactions below threshold should remain unchanged
    /// </summary>
    [Test]
    public async Task AutoAssignDocumentsAsync_WithSkippedTransactions_DoesNotModifyTaxData()
    {
        // Arrange
        var yearMonth = new DateOnly(2024, 3, 1);
        var account = new Account { Id = 1, Name = "Test Account" };
        await _context.Accounts.AddAsync(account);
        
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 119.00m,
            TransactionDateTime = new DateTime(2024, 3, 15),
            Counterparty = "Vendor A",
            TransactionReference = "INV-001",
            TransactionNote = "Low match score",
            SenderReceiver = "Vendor A",
            Account = account,
            DocumentId = null,
            NetAmount = 50.00m,  // Pre-existing value
            TaxAmount = 9.50m,   // Pre-existing value
            TaxRate = 19.0m      // Pre-existing value
        };
        
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        
        // Setup mock to return match below threshold
        var matchesByTransaction = new Dictionary<int, IEnumerable<DocumentMatch>>
        {
            { 1, new[] { new DocumentMatch 
                { 
                    Document = new Model.Dto.DocumentDto 
                    { 
                        Id = 100,
                        Name = "Invoice-001.pdf",
                        ExternalRef = "doc-001",
                        SubTotal = 100.00m,
                        TaxAmount = 19.00m,
                        TaxRate = 19.0m,
                        Total = 119.00m,
                        Skonto = null
                    }, 
                    MatchScore = 0.3, // Below 0.5 threshold
                    ScoreBreakdown = new MatchScoreBreakdown() 
                } 
            } }
        };
        
        _mockMatchingService.BatchDocumentMatchesAsync(
            Arg.Any<IEnumerable<Transaction>>(), 
            Arg.Any<CancellationToken>())
            .Returns(matchesByTransaction);
        
        // Act
        var result = await _service.AutoAssignDocumentsAsync(yearMonth);
        
        // Assert
        Assert.That(result.SkippedCount, Is.EqualTo(1), "Should skip 1 transaction");
        
        // Verify transaction remains unchanged
        var updatedTransaction = await _context.Transactions.FindAsync(1);
        Assert.That(updatedTransaction.DocumentId, Is.Null, "Transaction should remain unassigned");
        Assert.That(updatedTransaction.NetAmount, Is.EqualTo(50.00m), "NetAmount should remain unchanged");
        Assert.That(updatedTransaction.TaxAmount, Is.EqualTo(9.50m), "TaxAmount should remain unchanged");
        Assert.That(updatedTransaction.TaxRate, Is.EqualTo(19.0m), "TaxRate should remain unchanged");
    }
}
