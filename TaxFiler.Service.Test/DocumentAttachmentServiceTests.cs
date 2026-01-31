using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class DocumentAttachmentServiceTests
{
    private TaxFilerContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TaxFilerContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new TestTaxFilerContext(options);
    }

    [Test]
    public async Task AttachDocumentAsync_ValidTransactionAndDocument_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var account = new Account { Name = "Test Account" };
        var document = new Document("Test Document", "REF001", false, 19m, 19m, 100m, 81m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        var transaction = new Transaction
        {
            Account = account,
            GrossAmount = 100m,
            Counterparty = "Test Counterparty",
            TransactionReference = "TXN001",
            TransactionDateTime = DateTime.Now,
            TransactionNote = "Test transaction",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Documents.Add(document);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AttachDocumentAsync(transaction.Id, document.Id, false, "TestUser");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        var attachment = await context.DocumentAttachments
            .FirstOrDefaultAsync(da => da.TransactionId == transaction.Id && da.DocumentId == document.Id);
        
        Assert.That(attachment, Is.Not.Null);
        Assert.That(attachment.AttachedBy, Is.EqualTo("TestUser"));
        Assert.That(attachment.IsAutomatic, Is.False);
    }

    [Test]
    public async Task AttachDocumentAsync_NonExistentTransaction_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var document = new Document("Test Document", "REF001", false, 19m, 19m, 100m, 81m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AttachDocumentAsync(999, document.Id);

        // Assert
        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.First().Message, Does.Contain("Transaction with ID 999 not found"));
    }

    [Test]
    public async Task AttachDocumentAsync_NonExistentDocument_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var account = new Account { Name = "Test Account" };
        var transaction = new Transaction
        {
            Account = account,
            GrossAmount = 100m,
            Counterparty = "Test Counterparty",
            TransactionReference = "TXN001",
            TransactionDateTime = DateTime.Now,
            TransactionNote = "Test transaction",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AttachDocumentAsync(transaction.Id, 999);

        // Assert
        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.First().Message, Does.Contain("Document with ID 999 not found"));
    }

    [Test]
    public async Task AttachDocumentAsync_DuplicateAttachment_ReturnsFailure()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var account = new Account { Name = "Test Account" };
        var document = new Document("Test Document", "REF001", false, 19m, 19m, 100m, 81m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        var transaction = new Transaction
        {
            Account = account,
            GrossAmount = 100m,
            Counterparty = "Test Counterparty",
            TransactionReference = "TXN001",
            TransactionDateTime = DateTime.Now,
            TransactionNote = "Test transaction",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Documents.Add(document);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Create initial attachment
        await service.AttachDocumentAsync(transaction.Id, document.Id);

        // Act - Try to attach the same document again
        var result = await service.AttachDocumentAsync(transaction.Id, document.Id);

        // Assert
        Assert.That(result.IsFailed, Is.True);
        Assert.That(result.Errors.First().Message, Does.Contain("already attached"));
    }

    [Test]
    public async Task GetAttachedDocumentsAsync_ValidTransaction_ReturnsAttachedDocuments()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var account = new Account { Name = "Test Account" };
        var document1 = new Document("Document 1", "REF001", false, 19m, 19m, 50m, 31m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        var document2 = new Document("Document 2", "REF002", false, 19m, 19m, 50m, 31m, DateOnly.FromDateTime(DateTime.Today), "INV002", true, 0m);
        var transaction = new Transaction
        {
            Account = account,
            GrossAmount = 100m,
            Counterparty = "Test Counterparty",
            TransactionReference = "TXN001",
            TransactionDateTime = DateTime.Now,
            TransactionNote = "Test transaction",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Documents.AddRange(document1, document2);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Attach both documents
        await service.AttachDocumentAsync(transaction.Id, document1.Id);
        await service.AttachDocumentAsync(transaction.Id, document2.Id);

        // Act
        var result = await service.GetAttachedDocumentsAsync(transaction.Id);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Count(), Is.EqualTo(2));
        
        var documentNames = result.Value.Select(d => d.Name).OrderBy(n => n).ToList();
        Assert.That(documentNames, Is.EqualTo(new[] { "Document 1", "Document 2" }));
    }

    [Test]
    public async Task DetachDocumentAsync_ValidAttachment_RemovesAttachment()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var account = new Account { Name = "Test Account" };
        var document = new Document("Test Document", "REF001", false, 19m, 19m, 100m, 81m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        var transaction = new Transaction
        {
            Account = account,
            GrossAmount = 100m,
            Counterparty = "Test Counterparty",
            TransactionReference = "TXN001",
            TransactionDateTime = DateTime.Now,
            TransactionNote = "Test transaction",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Documents.Add(document);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Attach document first
        await service.AttachDocumentAsync(transaction.Id, document.Id);

        // Verify attachment exists
        var attachmentBefore = await context.DocumentAttachments
            .FirstOrDefaultAsync(da => da.TransactionId == transaction.Id && da.DocumentId == document.Id);
        Assert.That(attachmentBefore, Is.Not.Null);

        // Act
        var result = await service.DetachDocumentAsync(transaction.Id, document.Id);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        var attachmentAfter = await context.DocumentAttachments
            .FirstOrDefaultAsync(da => da.TransactionId == transaction.Id && da.DocumentId == document.Id);
        Assert.That(attachmentAfter, Is.Null);
    }

    [Test]
    public async Task GetAttachmentSummaryAsync_ValidTransaction_ReturnsCorrectSummary()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var account = new Account { Name = "Test Account" };
        var document1 = new Document("Document 1", "REF001", false, 19m, 19m, 40m, 21m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        var document2 = new Document("Document 2", "REF002", false, 19m, 19m, 60m, 41m, DateOnly.FromDateTime(DateTime.Today), "INV002", true, 0m);
        var transaction = new Transaction
        {
            Account = account,
            GrossAmount = 100m,
            Counterparty = "Test Counterparty",
            TransactionReference = "TXN001",
            TransactionDateTime = DateTime.Now,
            TransactionNote = "Test transaction",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Documents.AddRange(document1, document2);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Attach both documents
        await service.AttachDocumentAsync(transaction.Id, document1.Id);
        await service.AttachDocumentAsync(transaction.Id, document2.Id);

        // Act
        var result = await service.GetAttachmentSummaryAsync(transaction.Id);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        var summary = result.Value;
        Assert.That(summary.TransactionId, Is.EqualTo(transaction.Id));
        Assert.That(summary.AttachedDocumentCount, Is.EqualTo(2));
        Assert.That(summary.TotalAttachedAmount, Is.EqualTo(100m)); // 40 + 60
        Assert.That(summary.TransactionAmount, Is.EqualTo(100m));
        Assert.That(summary.AmountDifference, Is.EqualTo(0m));
        Assert.That(summary.HasAmountMismatch, Is.False);
        Assert.That(summary.AttachedDocuments.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAttachmentSummaryAsync_AmountMismatch_DetectsMismatch()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var logger = Substitute.For<ILogger<DocumentAttachmentService>>();
        var service = new DocumentAttachmentService(context, logger);
        
        var account = new Account { Name = "Test Account" };
        var document = new Document("Test Document", "REF001", false, 19m, 19m, 150m, 131m, DateOnly.FromDateTime(DateTime.Today), "INV001", true, 0m);
        var transaction = new Transaction
        {
            Account = account,
            GrossAmount = 100m,
            Counterparty = "Test Counterparty",
            TransactionReference = "TXN001",
            TransactionDateTime = DateTime.Now,
            TransactionNote = "Test transaction",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Documents.Add(document);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Attach document
        await service.AttachDocumentAsync(transaction.Id, document.Id);

        // Act
        var result = await service.GetAttachmentSummaryAsync(transaction.Id);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        var summary = result.Value;
        Assert.That(summary.TotalAttachedAmount, Is.EqualTo(150m));
        Assert.That(summary.TransactionAmount, Is.EqualTo(100m));
        Assert.That(summary.AmountDifference, Is.EqualTo(50m));
        Assert.That(summary.HasAmountMismatch, Is.True);
    }

    // Test helper class to allow constructor injection for testing
    private class TestTaxFilerContext : TaxFilerContext
    {
        public TestTaxFilerContext(DbContextOptions<TaxFilerContext> options) 
            : base(CreateTestConfiguration())
        {
            // Override the options for testing
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
            // Don't call base.OnConfiguring to avoid using the real connection string
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("TestDatabase");
            }
        }
    }
}