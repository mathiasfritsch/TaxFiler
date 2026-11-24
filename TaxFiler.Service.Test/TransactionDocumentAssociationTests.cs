using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NSubstitute;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service.Test;

/// <summary>
/// Tests for the many-to-many transaction-document association. A document may be
/// linked to many transactions and a transaction may be linked to many documents.
/// </summary>
[TestFixture]
public class TransactionDocumentAssociationTests
{
    private TestTaxFilerContext _context;
    private TransactionService _service;

    [SetUp]
    public void Setup()
    {
        _context = new TestTaxFilerContext();
        _service = new TransactionService(_context, Substitute.For<IDocumentMatchingService>());
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    private static Document NewDocument(string name = "Invoice-001.pdf", decimal subTotal = 100m, decimal taxAmount = 19m, decimal taxRate = 19m)
        => new(name, $"ref-{name}", false, taxRate, taxAmount, subTotal + taxAmount, subTotal,
            DateOnly.FromDateTime(new DateTime(2024, 3, 15)), "INV-001", true, null, "Test Vendor");

    private async Task<Transaction> AddTransactionAsync(decimal grossAmount = 119m)
    {
        var account = new Account { Name = "Test Account" };
        await _context.Accounts.AddAsync(account);

        var transaction = new Transaction
        {
            GrossAmount = grossAmount,
            Counterparty = "Test Vendor",
            TransactionReference = "INV-001",
            TransactionNote = "Test transaction",
            SenderReceiver = "Test Vendor",
            Account = account
        };
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    [Test]
    public async Task AddDocumentToTransactionAsync_FirstDocument_GetsSequenceOne()
    {
        var transaction = await AddTransactionAsync();
        var document = NewDocument();
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        var result = await _service.AddDocumentToTransactionAsync(
            transaction.Id, new AddDocumentToTransactionDto { DocumentId = document.Id });

        Assert.That(result.IsSuccess, Is.True, result.Errors.FirstOrDefault()?.Message);
        Assert.That(result.Value.Sequence, Is.EqualTo(1), "First document should be sequence 1");

        var links = await _service.GetTransactionDocumentsAsync(transaction.Id);
        Assert.That(links.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task AddDocumentToTransactionAsync_SecondDocument_GetsSequenceTwo()
    {
        var transaction = await AddTransactionAsync();
        var doc1 = NewDocument("Doc1");
        var doc2 = NewDocument("Doc2");
        await _context.Documents.AddRangeAsync(doc1, doc2);
        await _context.SaveChangesAsync();

        await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = doc1.Id });
        await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = doc2.Id });

        var links = (await _service.GetTransactionDocumentsAsync(transaction.Id)).ToList();
        Assert.That(links.Select(l => l.Sequence), Is.EquivalentTo(new[] { 1, 2 }), "Documents should be appended in order");
    }

    [Test]
    public async Task AddDocumentToTransactionAsync_DuplicateLinkToSameTransaction_Fails()
    {
        var transaction = await AddTransactionAsync();
        var document = NewDocument();
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = document.Id });
        var duplicate = await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = document.Id });

        Assert.That(duplicate.IsFailed, Is.True, "Linking the same document twice should fail");
    }

    [Test]
    public async Task AddDocumentToTransactionAsync_SameDocument_CanLinkToMultipleTransactions()
    {
        var transaction1 = await AddTransactionAsync();
        var transaction2 = await AddTransactionAsync();
        var document = NewDocument();
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        var r1 = await _service.AddDocumentToTransactionAsync(transaction1.Id, new AddDocumentToTransactionDto { DocumentId = document.Id });
        var r2 = await _service.AddDocumentToTransactionAsync(transaction2.Id, new AddDocumentToTransactionDto { DocumentId = document.Id });

        Assert.That(r1.IsSuccess, Is.True);
        Assert.That(r2.IsSuccess, Is.True, "A document may be linked to multiple transactions");

        var doc = await _context.Documents.Include(d => d.TransactionDocuments).SingleAsync(d => d.Id == document.Id);
        Assert.That(doc.TransactionDocuments.Select(td => td.TransactionId), Is.EquivalentTo(new[] { transaction1.Id, transaction2.Id }));
    }

    [Test]
    public async Task RemoveDocumentFromTransactionAsync_RemovesLink_KeepsRecords()
    {
        var transaction = await AddTransactionAsync();
        var document = NewDocument();
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = document.Id });

        var result = await _service.RemoveDocumentFromTransactionAsync(transaction.Id, document.Id);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await _service.GetTransactionDocumentsAsync(transaction.Id), Is.Empty);

        Assert.That(await _context.Transactions.FindAsync(transaction.Id), Is.Not.Null, "Transaction must remain");
        Assert.That(await _context.Documents.FindAsync(document.Id), Is.Not.Null, "Document must remain");
    }

    [Test]
    public async Task RemoveDocumentFromTransactionAsync_NotLinked_Fails()
    {
        var transaction = await AddTransactionAsync();
        var document = NewDocument();
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        var result = await _service.RemoveDocumentFromTransactionAsync(transaction.Id, document.Id);

        Assert.That(result.IsFailed, Is.True);
    }

    [Test]
    public async Task AddDocumentToTransactionAsync_PrimaryDocument_CopiesTaxData()
    {
        var transaction = await AddTransactionAsync(grossAmount: 119m);
        var document = NewDocument(subTotal: 100m, taxAmount: 19m, taxRate: 19m);
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();

        var result = await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = document.Id });

        Assert.That(result.IsSuccess, Is.True);

        var updated = await _context.Transactions.FindAsync(transaction.Id);
        Assert.That(updated!.NetAmount, Is.EqualTo(100m), "Primary document should copy SubTotal to NetAmount");
        Assert.That(updated.TaxAmount, Is.EqualTo(19m), "Primary document should copy TaxAmount");
        Assert.That(updated.TaxRate, Is.EqualTo(19m), "Primary document should copy TaxRate");
    }

    [Test]
    public async Task GetTransactionDocumentsAsync_ReturnsDocumentsInSequenceOrder()
    {
        var transaction = await AddTransactionAsync();
        var doc1 = NewDocument("Doc1");
        var doc2 = NewDocument("Doc2");
        var doc3 = NewDocument("Doc3");
        await _context.Documents.AddRangeAsync(doc1, doc2, doc3);
        await _context.SaveChangesAsync();

        await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = doc1.Id });
        await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = doc2.Id });
        await _service.AddDocumentToTransactionAsync(transaction.Id, new AddDocumentToTransactionDto { DocumentId = doc3.Id });

        var links = (await _service.GetTransactionDocumentsAsync(transaction.Id)).ToList();

        Assert.That(links.Select(l => l.DocumentId), Is.EqualTo(new[] { doc1.Id, doc2.Id, doc3.Id }));
    }
}
