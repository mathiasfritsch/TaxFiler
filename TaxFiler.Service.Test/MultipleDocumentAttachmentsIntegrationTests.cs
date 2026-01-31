using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

/// <summary>
/// Integration tests for the complete multiple document attachments workflow.
/// Tests end-to-end scenarios from API to database including auto-assignment and complex matching.
/// </summary>
[TestFixture]
public class MultipleDocumentAttachmentsIntegrationTests
{
    private TaxFilerContext _context = null!;
    private IDocumentAttachmentService _attachmentService = null!;
    private IDocumentMatchingService _matchingService = null!;
    private ILogger<DocumentAttachmentService> _attachmentLogger = null!;
    private ILogger<DocumentMatchingService> _matchingLogger = null!;

    [SetUp]
    public void Setup()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<TaxFilerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var configuration = Substitute.For<IConfiguration>();
        _context = new TaxFilerContext(configuration);
        _context.Database.EnsureCreated();

        // Create loggers
        _attachmentLogger = Substitute.For<ILogger<DocumentAttachmentService>>();
        _matchingLogger = Substitute.For<ILogger<DocumentMatchingService>>();

        // Create services
        _attachmentService = new DocumentAttachmentService(_context, _attachmentLogger);
        
        // Create matching services with dependencies
        var config = new MatchingConfiguration();
        var amountMatcher = new AmountMatcher();
        var dateMatcher = new DateMatcher();
        var vendorMatcher = new VendorMatcher();
        var referenceMatcher = new ReferenceMatcher();
        
        _matchingService = new DocumentMatchingService(
            _context, config, amountMatcher, dateMatcher, vendorMatcher, referenceMatcher, 
            _attachmentService, _matchingLogger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task CompleteAttachmentWorkflow_SingleTransaction_MultipleDocuments_Success()
    {
        // Arrange: Create test data
        var account = new Account { Id = 1, Name = "Test Account" };
        var transaction = new Transaction
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = -150.00m,
            Counterparty = "Test Vendor",
            TransactionNote = "Payment for INV001, INV002",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true
        };

        var document1 = new Document
        {
            Id = 1,
            Name = "Invoice 001",
            InvoiceNumber = "INV001",
            Total = 75.00m,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            VendorName = "Test Vendor"
        };

        var document2 = new Document
        {
            Id = 2,
            Name = "Invoice 002", 
            InvoiceNumber = "INV002",
            Total = 75.00m,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            VendorName = "Test Vendor"
        };

        _context.Accounts.Add(account);
        _context.Transactions.Add(transaction);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act: Test complete workflow
        
        // 1. Attach first document
        var attachResult1 = await _attachmentService.AttachDocumentAsync(1, 1, false, "TestUser");
        
        // 2. Attach second document
        var attachResult2 = await _attachmentService.AttachDocumentAsync(1, 2, false, "TestUser");
        
        // 3. Get attachment summary
        var summaryResult = await _attachmentService.GetAttachmentSummaryAsync(1);
        
        // 4. Get attached documents
        var documentsResult = await _attachmentService.GetAttachedDocumentsAsync(1);
        
        // 5. Get attachment history
        var historyResult = await _attachmentService.GetAttachmentHistoryAsync(1);

        // Assert: Verify all operations succeeded
        Assert.That(attachResult1.IsSuccess, Is.True, "First attachment should succeed");
        Assert.That(attachResult2.IsSuccess, Is.True, "Second attachment should succeed");
        Assert.That(summaryResult.IsSuccess, Is.True, "Summary retrieval should succeed");
        Assert.That(documentsResult.IsSuccess, Is.True, "Documents retrieval should succeed");
        Assert.That(historyResult.IsSuccess, Is.True, "History retrieval should succeed");

        // Verify summary data
        var summary = summaryResult.Value;
        Assert.That(summary.AttachedDocumentCount, Is.EqualTo(2), "Should have 2 attached documents");
        Assert.That(summary.TotalAttachedAmount, Is.EqualTo(150.00m), "Total amount should be 150.00");
        Assert.That(summary.TransactionAmount, Is.EqualTo(150.00m), "Transaction amount should be 150.00");
        Assert.That(summary.HasAmountMismatch, Is.False, "Should not have amount mismatch");

        // Verify attached documents
        var attachedDocs = documentsResult.Value.ToList();
        Assert.That(attachedDocs.Count, Is.EqualTo(2), "Should return 2 attached documents");
        Assert.That(attachedDocs.Any(d => d.InvoiceNumber == "INV001"), Is.True, "Should include INV001");
        Assert.That(attachedDocs.Any(d => d.InvoiceNumber == "INV002"), Is.True, "Should include INV002");

        // Verify attachment history
        var history = historyResult.Value.ToList();
        Assert.That(history.Count, Is.EqualTo(2), "Should have 2 history entries");
        Assert.That(history.All(h => h.TransactionId == 1), Is.True, "All history should be for transaction 1");
        Assert.That(history.All(h => h.AttachedBy == "TestUser"), Is.True, "All attachments should be by TestUser");
    }

    [Test]
    public async Task AutoAssignMultipleDocuments_ComplexScenario_Success()
    {
        // Arrange: Create complex test scenario with multiple matching strategies
        var account = new Account { Id = 1, Name = "Test Account" };
        var transaction = new Transaction
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = -225.50m,
            Counterparty = "ABC Company",
            TransactionNote = "Payment for invoices REF123, REF124, REF125",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true
        };

        var documents = new[]
        {
            new Document
            {
                Id = 1,
                Name = "Invoice REF123",
                InvoiceNumber = "REF123",
                Total = 100.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                VendorName = "ABC Company"
            },
            new Document
            {
                Id = 2,
                Name = "Invoice REF124",
                InvoiceNumber = "REF124", 
                Total = 75.25m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                VendorName = "ABC Company"
            },
            new Document
            {
                Id = 3,
                Name = "Invoice REF125",
                InvoiceNumber = "REF125",
                Total = 50.25m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
                VendorName = "ABC Company"
            },
            new Document
            {
                Id = 4,
                Name = "Unrelated Invoice",
                InvoiceNumber = "OTHER001",
                Total = 200.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                VendorName = "Other Company"
            }
        };

        _context.Accounts.Add(account);
        _context.Transactions.Add(transaction);
        _context.Documents.AddRange(documents);
        await _context.SaveChangesAsync();

        // Act: Test auto-assignment
        var autoAssignResult = await _matchingService.AutoAssignMultipleDocumentsAsync(1);

        // Assert: Verify auto-assignment succeeded
        Assert.That(autoAssignResult.IsSuccess, Is.True, "Auto-assignment should succeed");
        
        var result = autoAssignResult.Value;
        Assert.That(result.DocumentsAttached, Is.EqualTo(3), "Should attach 3 documents");
        Assert.That(result.TotalAmount, Is.EqualTo(225.50m), "Total amount should match transaction");
        Assert.That(result.HasWarnings, Is.False, "Should not have warnings for exact match");

        // Verify documents were actually attached in database
        var attachments = await _context.DocumentAttachments
            .Where(da => da.TransactionId == 1)
            .Include(da => da.Document)
            .ToListAsync();

        Assert.That(attachments.Count, Is.EqualTo(3), "Should have 3 attachments in database");
        Assert.That(attachments.All(a => a.IsAutomatic), Is.True, "All attachments should be automatic");
        
        var attachedInvoiceNumbers = attachments.Select(a => a.Document.InvoiceNumber).ToList();
        Assert.That(attachedInvoiceNumbers.Contains("REF123"), Is.True, "Should include REF123");
        Assert.That(attachedInvoiceNumbers.Contains("REF124"), Is.True, "Should include REF124");
        Assert.That(attachedInvoiceNumbers.Contains("REF125"), Is.True, "Should include REF125");
        Assert.That(attachedInvoiceNumbers.Contains("OTHER001"), Is.False, "Should not include unrelated document");
    }

    [Test]
    public async Task MultipleDocumentMatching_VariousStrategies_ReturnsRankedResults()
    {
        // Arrange: Create test data for different matching strategies
        var account = new Account { Id = 1, Name = "Test Account" };
        var transaction = new Transaction
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = -300.00m,
            Counterparty = "Multi Vendor",
            TransactionNote = "Payment for A001, A002 and other expenses",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true
        };

        var documents = new[]
        {
            // Perfect reference match combination
            new Document
            {
                Id = 1,
                Name = "Invoice A001",
                InvoiceNumber = "A001",
                Total = 150.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                VendorName = "Multi Vendor"
            },
            new Document
            {
                Id = 2,
                Name = "Invoice A002",
                InvoiceNumber = "A002",
                Total = 150.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
                VendorName = "Multi Vendor"
            },
            // Amount-based match combination
            new Document
            {
                Id = 3,
                Name = "Invoice B001",
                InvoiceNumber = "B001",
                Total = 100.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                VendorName = "Multi Vendor"
            },
            new Document
            {
                Id = 4,
                Name = "Invoice B002",
                InvoiceNumber = "B002",
                Total = 200.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)),
                VendorName = "Multi Vendor"
            }
        };

        _context.Accounts.Add(account);
        _context.Transactions.Add(transaction);
        _context.Documents.AddRange(documents);
        await _context.SaveChangesAsync();

        // Act: Find multiple document matches
        var matches = await _matchingService.FindMultipleDocumentMatchesAsync(1);

        // Assert: Verify matching results
        var matchList = matches.ToList();
        Assert.That(matchList.Count, Is.GreaterThan(0), "Should find multiple document matches");

        // Verify the best match (should be reference-based A001+A002)
        var bestMatch = matchList.First();
        Assert.That(bestMatch.DocumentCount, Is.EqualTo(2), "Best match should have 2 documents");
        Assert.That(bestMatch.TotalDocumentAmount, Is.EqualTo(300.00m), "Best match should total 300.00");
        Assert.That(bestMatch.MatchScore, Is.GreaterThan(0.7), "Best match should have high score");

        var bestMatchInvoices = bestMatch.Documents.Select(d => d.InvoiceNumber).ToList();
        Assert.That(bestMatchInvoices.Contains("A001"), Is.True, "Best match should include A001");
        Assert.That(bestMatchInvoices.Contains("A002"), Is.True, "Best match should include A002");

        // Verify matches are ordered by score
        for (int i = 1; i < matchList.Count; i++)
        {
            Assert.That(matchList[i].MatchScore, Is.LessThanOrEqualTo(matchList[i-1].MatchScore), 
                "Matches should be ordered by score descending");
        }
    }

    [Test]
    public async Task AttachmentValidation_BusinessRules_EnforcedCorrectly()
    {
        // Arrange: Create test data for business rule validation
        var account = new Account { Id = 1, Name = "Test Account" };
        var transaction1 = new Transaction
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = -100.00m,
            Counterparty = "Test Vendor",
            TransactionNote = "Payment 1",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true
        };

        var transaction2 = new Transaction
        {
            Id = 2,
            AccountId = 1,
            GrossAmount = -50.00m,
            Counterparty = "Test Vendor",
            TransactionNote = "Payment 2",
            TransactionDateTime = DateTime.UtcNow.AddDays(1),
            IsOutgoing = true
        };

        var document = new Document
        {
            Id = 1,
            Name = "Shared Invoice",
            InvoiceNumber = "SHARED001",
            Total = 75.00m,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            VendorName = "Test Vendor"
        };

        _context.Accounts.Add(account);
        _context.Transactions.AddRange(transaction1, transaction2);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act & Assert: Test business rules

        // 1. Test successful attachment
        var attachResult1 = await _attachmentService.AttachDocumentAsync(1, 1, false, "TestUser");
        Assert.That(attachResult1.IsSuccess, Is.True, "First attachment should succeed");

        // 2. Test duplicate attachment prevention (Business Rule 5.1)
        var duplicateResult = await _attachmentService.AttachDocumentAsync(1, 1, false, "TestUser");
        Assert.That(duplicateResult.IsFailed, Is.True, "Duplicate attachment should fail");
        Assert.That(duplicateResult.Errors.Any(e => e.Message.Contains("already attached")), Is.True, 
            "Should indicate document is already attached");

        // 3. Test attachment to different transaction (should warn but allow - Business Rule 5.4)
        var crossAttachResult = await _attachmentService.AttachDocumentAsync(2, 1, false, "TestUser");
        Assert.That(crossAttachResult.IsSuccess, Is.True, "Cross-attachment should succeed");
        // Note: Warnings are included in the result but don't make it fail

        // 4. Test amount overage warning (Business Rule 5.2)
        var largeDocument = new Document
        {
            Id = 2,
            Name = "Large Invoice",
            InvoiceNumber = "LARGE001",
            Total = 200.00m, // Much larger than transaction amount
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            VendorName = "Test Vendor"
        };

        _context.Documents.Add(largeDocument);
        await _context.SaveChangesAsync();

        var overageResult = await _attachmentService.AttachDocumentAsync(1, 2, false, "TestUser");
        Assert.That(overageResult.IsSuccess, Is.True, "Overage attachment should succeed with warning");
        // The service should log warnings but still allow the attachment

        // 5. Test non-existent document/transaction
        var nonExistentDocResult = await _attachmentService.AttachDocumentAsync(1, 999, false, "TestUser");
        Assert.That(nonExistentDocResult.IsFailed, Is.True, "Non-existent document should fail");

        var nonExistentTransResult = await _attachmentService.AttachDocumentAsync(999, 1, false, "TestUser");
        Assert.That(nonExistentTransResult.IsFailed, Is.True, "Non-existent transaction should fail");
    }

    [Test]
    public async Task PaginatedAttachments_LargeDataSet_WorksCorrectly()
    {
        // Arrange: Create transaction with many documents
        var account = new Account { Id = 1, Name = "Test Account" };
        var transaction = new Transaction
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = -1000.00m,
            Counterparty = "Big Vendor",
            TransactionNote = "Large payment",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true
        };

        _context.Accounts.Add(account);
        _context.Transactions.Add(transaction);

        // Create 25 documents and attach them
        var documents = new List<Document>();
        for (int i = 1; i <= 25; i++)
        {
            var document = new Document
            {
                Id = i,
                Name = $"Invoice {i:D3}",
                InvoiceNumber = $"INV{i:D3}",
                Total = 40.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                VendorName = "Big Vendor"
            };
            documents.Add(document);
        }

        _context.Documents.AddRange(documents);
        await _context.SaveChangesAsync();

        // Attach all documents
        for (int i = 1; i <= 25; i++)
        {
            await _attachmentService.AttachDocumentAsync(1, i, false, "TestUser");
        }

        // Act: Test pagination
        var page1Result = await _attachmentService.GetAttachedDocumentsPaginatedAsync(1, 1, 10);
        var page2Result = await _attachmentService.GetAttachedDocumentsPaginatedAsync(1, 2, 10);
        var page3Result = await _attachmentService.GetAttachedDocumentsPaginatedAsync(1, 3, 10);

        // Assert: Verify pagination works correctly
        Assert.That(page1Result.IsSuccess, Is.True, "Page 1 should succeed");
        Assert.That(page2Result.IsSuccess, Is.True, "Page 2 should succeed");
        Assert.That(page3Result.IsSuccess, Is.True, "Page 3 should succeed");

        var page1 = page1Result.Value;
        var page2 = page2Result.Value;
        var page3 = page3Result.Value;

        // Verify page 1
        Assert.That(page1.Items.Count(), Is.EqualTo(10), "Page 1 should have 10 items");
        Assert.That(page1.PageNumber, Is.EqualTo(1), "Page 1 should have correct page number");
        Assert.That(page1.PageSize, Is.EqualTo(10), "Page 1 should have correct page size");
        Assert.That(page1.TotalCount, Is.EqualTo(25), "Page 1 should have correct total count");
        Assert.That(page1.TotalPages, Is.EqualTo(3), "Page 1 should have correct total pages");
        Assert.That(page1.HasPreviousPage, Is.False, "Page 1 should not have previous page");
        Assert.That(page1.HasNextPage, Is.True, "Page 1 should have next page");

        // Verify page 2
        Assert.That(page2.Items.Count(), Is.EqualTo(10), "Page 2 should have 10 items");
        Assert.That(page2.HasPreviousPage, Is.True, "Page 2 should have previous page");
        Assert.That(page2.HasNextPage, Is.True, "Page 2 should have next page");

        // Verify page 3
        Assert.That(page3.Items.Count(), Is.EqualTo(5), "Page 3 should have 5 items");
        Assert.That(page3.HasPreviousPage, Is.True, "Page 3 should have previous page");
        Assert.That(page3.HasNextPage, Is.False, "Page 3 should not have next page");

        // Verify no duplicate items across pages
        var allItems = page1.Items.Concat(page2.Items).Concat(page3.Items).ToList();
        var uniqueIds = allItems.Select(d => d.Id).Distinct().ToList();
        Assert.That(uniqueIds.Count, Is.EqualTo(25), "Should have 25 unique documents across all pages");
    }

    [Test]
    public async Task DetachDocument_CompleteWorkflow_Success()
    {
        // Arrange: Create transaction with attached documents
        var account = new Account { Id = 1, Name = "Test Account" };
        var transaction = new Transaction
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = -200.00m,
            Counterparty = "Test Vendor",
            TransactionNote = "Test payment",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true
        };

        var documents = new[]
        {
            new Document
            {
                Id = 1,
                Name = "Invoice 1",
                InvoiceNumber = "INV001",
                Total = 100.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                VendorName = "Test Vendor"
            },
            new Document
            {
                Id = 2,
                Name = "Invoice 2",
                InvoiceNumber = "INV002",
                Total = 100.00m,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
                VendorName = "Test Vendor"
            }
        };

        _context.Accounts.Add(account);
        _context.Transactions.Add(transaction);
        _context.Documents.AddRange(documents);
        await _context.SaveChangesAsync();

        // Attach both documents
        await _attachmentService.AttachDocumentAsync(1, 1, false, "TestUser");
        await _attachmentService.AttachDocumentAsync(1, 2, false, "TestUser");

        // Verify initial state
        var initialSummary = await _attachmentService.GetAttachmentSummaryAsync(1);
        Assert.That(initialSummary.Value.AttachedDocumentCount, Is.EqualTo(2), "Should start with 2 attachments");

        // Act: Detach one document
        var detachResult = await _attachmentService.DetachDocumentAsync(1, 1);

        // Assert: Verify detachment succeeded
        Assert.That(detachResult.IsSuccess, Is.True, "Detachment should succeed");

        // Verify updated state
        var updatedSummary = await _attachmentService.GetAttachmentSummaryAsync(1);
        Assert.That(updatedSummary.Value.AttachedDocumentCount, Is.EqualTo(1), "Should have 1 attachment after detach");
        Assert.That(updatedSummary.Value.TotalAttachedAmount, Is.EqualTo(100.00m), "Total amount should be updated");

        var remainingDocs = await _attachmentService.GetAttachedDocumentsAsync(1);
        var remainingList = remainingDocs.Value.ToList();
        Assert.That(remainingList.Count, Is.EqualTo(1), "Should have 1 remaining document");
        Assert.That(remainingList.First().InvoiceNumber, Is.EqualTo("INV002"), "Should keep INV002");

        // Verify document still exists (only attachment was removed)
        var document1 = await _context.Documents.FindAsync(1);
        Assert.That(document1, Is.Not.Null, "Document should still exist after detachment");

        // Test detaching non-existent attachment
        var nonExistentDetachResult = await _attachmentService.DetachDocumentAsync(1, 999);
        Assert.That(nonExistentDetachResult.IsFailed, Is.True, "Non-existent detachment should fail");
    }
}