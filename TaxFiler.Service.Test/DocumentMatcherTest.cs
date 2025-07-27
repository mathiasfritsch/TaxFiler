using System.Diagnostics;
using NSubstitute;
using TaxFiler.Model.Dto;
using TransactionDto = TaxFiler.Model.Csv.TransactionDto;


namespace TaxFiler.Service.Test;

[TestFixture]
public class DocumentMatcherTest
{
    private IDocumentService _mockDocumentService;
    private TransactionDocumentMatcherService _service;

    [SetUp]
    public void SetUp()
    {
        _mockDocumentService = Substitute.For<IDocumentService>();
        SetupGetAllUnmatchedDocumentsAsyncMock();
        _service = new TransactionDocumentMatcherService(_mockDocumentService);
    }

    [Test]
    public async Task TestMatchTransactionToDocumentAsync()
    {
        // Arrange
        
        var transaction = new TransactionDto
        {
            Amount = 238.0m,
            BookingDate = new DateTime(2024, 2, 10),
            Comment = "Payment inv GoodVendor I45789AB"
        };
        
        // Act
        
        var res = await _service.MatchTransactionToDocumentAsync(transaction);
        
        // Assert
        var matchedDocument = res;
        Debug.Assert(matchedDocument != null, nameof(matchedDocument) + " != null");
        Assert.That(matchedDocument.Id, Is.EqualTo(3), "Matched document ID should be 3");
    }
    
    private void SetupGetAllUnmatchedDocumentsAsyncMock()
    {
         var unmatchedDocuments = new DocumentDto[]
        {
            new DocumentDto
            {
                Id = 3,
                Name = "Bill_003.pdf",
                ExternalRef = "ext_003",
                Orphaned = false,
                Parsed = true,
                InvoiceNumber = "I45789AB",
                InvoiceDate = new DateOnly(2024, 2, 10),
                SubTotal = 200.0m,
                Total = 238.0m,
                TaxRate = 19.0m,
                TaxAmount = 38.0m,
                Skonto = 0m,
                Unconnected = true,
                VendorName = "GoodVendor",
            },
            new DocumentDto
            {
                Id = 4,
                Name = "Invoice_1B.pdf",
                ExternalRef = "ext_004",
                Orphaned = false,
                Parsed = true,
                InvoiceNumber = "INV-004",
                InvoiceDate = new DateOnly(2024, 2, 15),
                SubTotal = 300.0m,
                Total = 357.0m,
                TaxRate = 19.0m,
                TaxAmount = 57.0m,
                Skonto = 0m,
                Unconnected = true,
                VendorName = "AnotherVendor",
            },
    
            // 1. Exact amount match scenario - Document with total that exactly matches a transaction amount
            new DocumentDto
            {
                Id = 5,
                Name = "Invoice_005_ExactMatch.pdf",
                ExternalRef = "ext_005",
                Orphaned = false,
                Parsed = true,
                InvoiceNumber = "INV-2024-005",
                InvoiceDate = new DateOnly(2024, 3, 5),
                SubTotal = 75.0m,
                Total = 89.25m, // This amount should exactly match a transaction for testing
                TaxRate = 19.0m,
                TaxAmount = 14.25m,
                Skonto = 0m,
                Unconnected = true
            },
    
            // 2. Date proximity match scenario - Document with date close to a transaction date
            new DocumentDto
            {
                Id = 6,
                Name = "Receipt_006_DateMatch.pdf",
                ExternalRef = "ext_006",
                Orphaned = false,
                Parsed = true,
                InvoiceNumber = "REC-2024-006",
                InvoiceDate = new DateOnly(2024, 3, 12), // Date should be within 3 days of a transaction
                SubTotal = 150.0m,
                Total = 178.5m,
                TaxRate = 19.0m,
                TaxAmount = 28.5m,
                Skonto = 0m,
                Unconnected = true
            },
    
            // 3. Partial text match scenario - Document with vendor/reference that partially matches transaction
            new DocumentDto
            {
                Id = 7,
                Name = "Bill_007_TextMatch.pdf",
                ExternalRef = "ext_007",
                Orphaned = false,
                Parsed = true,
                InvoiceNumber = "BILL-ACME-2024-007", // Contains "ACME" for text matching
                InvoiceDate = new DateOnly(2024, 3, 20),
                SubTotal = 125.0m,
                Total = 148.75m,
                TaxRate = 19.0m,
                TaxAmount = 23.75m,
                Skonto = 0m,
                Unconnected = true
            }
        };
         
        _mockDocumentService.GetAllUnmatchedDocumentsAsync().Returns(Task.FromResult(unmatchedDocuments));
    }
}