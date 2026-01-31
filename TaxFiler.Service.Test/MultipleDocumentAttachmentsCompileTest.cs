using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

/// <summary>
/// Simple compilation test to verify the multiple document attachments functionality compiles and basic operations work.
/// </summary>
[TestFixture]
public class MultipleDocumentAttachmentsCompileTest
{
    [Test]
    public async Task MultipleDocumentAttachments_BasicOperations_Compile()
    {
        // Arrange: Create in-memory database
        var options = new DbContextOptionsBuilder<TaxFilerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestTaxFilerContext(options);

        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);

        // Create test data
        var account = new Account { Id = 1, Name = "Test Account" };
        var transaction = new Transaction
        {
            Id = 1,
            AccountId = 1,
            GrossAmount = -100.00m,
            Counterparty = "Test Vendor",
            TransactionNote = "Test payment",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        var document1 = new Document("Document 1", "REF001", false, 19m, 19m, 50m, 31m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        var document2 = new Document("Document 2", "REF002", false, 19m, 19m, 50m, 31m, DateOnly.FromDateTime(DateTime.Today), "INV002", true, 0m);

        context.Accounts.Add(account);
        context.Transactions.Add(transaction);
        context.Documents.AddRange(document1, document2);
        await context.SaveChangesAsync();

        // Act: Test basic operations
        var attachResult1 = await service.AttachDocumentAsync(1, 1, false, "TestUser");
        var attachResult2 = await service.AttachDocumentAsync(1, 2, false, "TestUser");
        var documentsResult = await service.GetAttachedDocumentsAsync(1);
        var summaryResult = await service.GetAttachmentSummaryAsync(1);

        // Assert: Verify operations completed (basic smoke test)
        Assert.That(attachResult1.IsSuccess, Is.True, "First attachment should succeed");
        Assert.That(attachResult2.IsSuccess, Is.True, "Second attachment should succeed");
        Assert.That(documentsResult.IsSuccess, Is.True, "Getting documents should succeed");
        Assert.That(summaryResult.IsSuccess, Is.True, "Getting summary should succeed");

        // Verify we can attach multiple documents
        var attachedDocs = documentsResult.Value.ToList();
        Assert.That(attachedDocs.Count, Is.EqualTo(2), "Should have 2 attached documents");

        var summary = summaryResult.Value;
        Assert.That(summary.AttachedDocumentCount, Is.EqualTo(2), "Summary should show 2 documents");
        Assert.That(summary.TotalAttachedAmount, Is.EqualTo(100.00m), "Total should be 100.00");
    }

    [Test]
    public void DocumentAttachment_EntityModel_HasCorrectProperties()
    {
        // Test that the DocumentAttachment entity has the expected properties
        var attachment = new DocumentAttachment
        {
            Id = 1,
            TransactionId = 1,
            DocumentId = 1,
            AttachedAt = DateTime.UtcNow,
            AttachedBy = "TestUser",
            IsAutomatic = false
        };

        Assert.That(attachment.Id, Is.EqualTo(1));
        Assert.That(attachment.TransactionId, Is.EqualTo(1));
        Assert.That(attachment.DocumentId, Is.EqualTo(1));
        Assert.That(attachment.AttachedBy, Is.EqualTo("TestUser"));
        Assert.That(attachment.IsAutomatic, Is.False);
    }

    [Test]
    public void Transaction_HasDocumentAttachmentsCollection()
    {
        // Test that Transaction entity has the DocumentAttachments collection
        var transaction = new Transaction
        {
            Id = 1,
            GrossAmount = 100m,
            Counterparty = "Test",
            TransactionNote = "Test",
            TransactionDateTime = DateTime.UtcNow,
            IsOutgoing = true,
            SenderReceiver = "Test"
        };

        Assert.That(transaction.DocumentAttachments, Is.Not.Null, "DocumentAttachments collection should exist");
        Assert.That(transaction.DocumentAttachments, Is.Empty, "DocumentAttachments collection should be empty initially");
    }

    [Test]
    public void Document_HasDocumentAttachmentsCollection()
    {
        // Test that Document entity has the DocumentAttachments collection
        var document = new Document("Test", "REF001", false, 19m, 19m, 100m, 81m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);

        Assert.That(document.DocumentAttachments, Is.Not.Null, "DocumentAttachments collection should exist");
        Assert.That(document.DocumentAttachments, Is.Empty, "DocumentAttachments collection should be empty initially");
    }

    // Test helper class to allow constructor injection for testing
    private class TestTaxFilerContext : TaxFilerContext
    {
        private readonly DbContextOptions<TaxFilerContext> _options;

        public TestTaxFilerContext(DbContextOptions<TaxFilerContext> options) 
            : base(CreateTestConfiguration())
        {
            _options = options;
            Database.EnsureCreated();
        }

        private static IConfiguration CreateTestConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TaxFilerNeonDB"] = "InMemory"
                })
                .Build();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use the provided options instead of the base configuration
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            }
        }
    }
}