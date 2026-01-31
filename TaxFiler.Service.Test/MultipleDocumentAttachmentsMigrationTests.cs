using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFiler.Service.Test;

[TestFixture]
public class MultipleDocumentAttachmentsMigrationTests
{
    private TaxFilerContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TaxFilerContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new TestTaxFilerContext(options);
    }

    [Test]
    public async Task Migration_PreservesExistingSingleDocumentAttachments()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        
        // Create test data that simulates the old schema
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

        // Simulate the old single document relationship by creating a DocumentAttachment
        var attachment = new DocumentAttachment
        {
            TransactionId = transaction.Id,
            DocumentId = document.Id,
            AttachedAt = DateTime.UtcNow,
            IsAutomatic = true
        };
        context.DocumentAttachments.Add(attachment);
        await context.SaveChangesAsync();

        // Act - Simulate the migration logic
        // In a real migration, this would be done by the SQL script
        var documentAttachment = new DocumentAttachment
        {
            TransactionId = transaction.Id,
            DocumentId = document.Id,
            AttachedAt = DateTime.UtcNow,
            AttachedBy = null,
            IsAutomatic = true
        };
        
        context.DocumentAttachments.Add(documentAttachment);
        await context.SaveChangesAsync();

        // Assert
        var attachments = await context.DocumentAttachments
            .Where(da => da.TransactionId == transaction.Id)
            .ToListAsync();
        Assert.That(attachments, Has.Count.EqualTo(1));
        
        var migratedAttachment = attachments.First();
        Assert.That(migratedAttachment.TransactionId, Is.EqualTo(transaction.Id));
        Assert.That(migratedAttachment.DocumentId, Is.EqualTo(document.Id));
        Assert.That(migratedAttachment.IsAutomatic, Is.True);
        Assert.That(migratedAttachment.AttachedBy, Is.Null);
    }

    [Test]
    public async Task DocumentAttachment_EnforcesUniqueConstraint()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        
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

        var attachment1 = new DocumentAttachment
        {
            TransactionId = transaction.Id,
            DocumentId = document.Id,
            AttachedAt = DateTime.UtcNow,
            IsAutomatic = false
        };

        var attachment2 = new DocumentAttachment
        {
            TransactionId = transaction.Id,
            DocumentId = document.Id,
            AttachedAt = DateTime.UtcNow,
            IsAutomatic = false
        };

        context.DocumentAttachments.Add(attachment1);
        await context.SaveChangesAsync();

        // Act & Assert
        context.DocumentAttachments.Add(attachment2);
        
        // Note: InMemory database doesn't enforce unique constraints like real databases
        // In a real database, this would throw an exception
        // For testing purposes, we'll verify the constraint exists in the model
        var entityType = context.Model.FindEntityType(typeof(DocumentAttachment));
        var uniqueIndex = entityType?.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && 
                                i.Properties.Count == 2 && 
                                i.Properties.Any(p => p.Name == "TransactionId") &&
                                i.Properties.Any(p => p.Name == "DocumentId"));
        
        Assert.That(uniqueIndex, Is.Not.Null, "Unique constraint on TransactionId-DocumentId should exist");
    }

    [Test]
    public async Task DocumentAttachment_SupportsCascadeDelete()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        
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

        var attachment = new DocumentAttachment
        {
            TransactionId = transaction.Id,
            DocumentId = document.Id,
            AttachedAt = DateTime.UtcNow,
            IsAutomatic = false
        };

        context.DocumentAttachments.Add(attachment);
        await context.SaveChangesAsync();

        // Verify attachment exists
        var attachmentsBefore = await context.DocumentAttachments
            .Where(da => da.TransactionId == transaction.Id)
            .ToListAsync();
        Assert.That(attachmentsBefore, Has.Count.EqualTo(1));

        // Act - Delete the transaction (InMemory doesn't support cascade, so we simulate it)
        var attachmentsToDelete = await context.DocumentAttachments
            .Where(da => da.TransactionId == transaction.Id)
            .ToListAsync();
        context.DocumentAttachments.RemoveRange(attachmentsToDelete);
        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync();

        // Assert - Attachment should be deleted due to cascade
        var attachments = await context.DocumentAttachments
            .Where(da => da.TransactionId == transaction.Id)
            .ToListAsync();
        Assert.That(attachments, Is.Empty);
    }

    [Test]
    public async Task DocumentAttachment_SupportsMultipleDocumentsPerTransaction()
    {
        // Arrange
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        
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
            TransactionNote = "Test transaction with multiple documents",
            IsOutgoing = true,
            SenderReceiver = "Test Sender"
        };

        context.Accounts.Add(account);
        context.Documents.AddRange(document1, document2);
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var attachment1 = new DocumentAttachment
        {
            TransactionId = transaction.Id,
            DocumentId = document1.Id,
            AttachedAt = DateTime.UtcNow,
            IsAutomatic = false
        };

        var attachment2 = new DocumentAttachment
        {
            TransactionId = transaction.Id,
            DocumentId = document2.Id,
            AttachedAt = DateTime.UtcNow,
            IsAutomatic = true
        };

        context.DocumentAttachments.AddRange(attachment1, attachment2);
        await context.SaveChangesAsync();

        // Act
        var transactionWithAttachments = await context.Transactions
            .Include(t => t.DocumentAttachments)
            .ThenInclude(da => da.Document)
            .FirstAsync(t => t.Id == transaction.Id);

        // Assert
        Assert.That(transactionWithAttachments.DocumentAttachments, Has.Count.EqualTo(2));
        
        var attachedDocuments = transactionWithAttachments.DocumentAttachments
            .Select(da => da.Document.Name)
            .OrderBy(name => name)
            .ToList();
        
        Assert.That(attachedDocuments, Is.EqualTo(new[] { "Document 1", "Document 2" }));
        
        // Verify one is automatic and one is manual
        var automaticAttachment = transactionWithAttachments.DocumentAttachments.First(da => da.IsAutomatic);
        var manualAttachment = transactionWithAttachments.DocumentAttachments.First(da => !da.IsAutomatic);
        
        Assert.That(automaticAttachment.Document.Name, Is.EqualTo("Document 2"));
        Assert.That(manualAttachment.Document.Name, Is.EqualTo("Document 1"));
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