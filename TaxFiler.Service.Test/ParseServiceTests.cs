using NUnit.Framework;
using TaxFiler.Service.LlamaIndex;

namespace TaxFiler.Service.Test;

[TestFixture]
public class ParseServiceTests
{
    [Test]
    public void InvoiceResult_ShouldHaveSkontoProperty()
    {
        // Arrange & Act
        var invoiceResult = new InvoiceResult
        {
            InvoiceNumber = "INV-123",
            TaxAmount = 19.00m,
            TaxRate = 19.00m,
            Total = 119.00m,
            SubTotal = 100.00m,
            InvoiceDate = "2024-01-15",
            Skonto = 3.00m // This should now be available
        };

        // Assert
        Assert.That(invoiceResult.Skonto, Is.EqualTo(3.00m));
    }

    [Test]
    public void InvoiceResult_SkontoCanBeNull()
    {
        // Arrange & Act
        var invoiceResult = new InvoiceResult
        {
            InvoiceNumber = "INV-456",
            TaxAmount = 19.00m,
            TaxRate = 19.00m,
            Total = 119.00m,
            SubTotal = 100.00m,
            InvoiceDate = "2024-01-15",
            Skonto = null // This should be allowed
        };

        // Assert
        Assert.That(invoiceResult.Skonto, Is.Null);
    }

    [Test]
    public void InvoiceResult_ShouldHaveMerchantProperty()
    {
        // Arrange & Act
        var merchant = new Merchant { Name = "Test Vendor GmbH" };
        var invoiceResult = new InvoiceResult
        {
            InvoiceNumber = "INV-789",
            TaxAmount = 19.00m,
            TaxRate = 19.00m,
            Total = 119.00m,
            SubTotal = 100.00m,
            InvoiceDate = "2024-01-15",
            Merchant = merchant
        };

        // Assert
        Assert.That(invoiceResult.Merchant, Is.Not.Null);
        Assert.That(invoiceResult.Merchant.Name, Is.EqualTo("Test Vendor GmbH"));
    }

    [Test]
    public void InvoiceResult_MerchantCanBeNull()
    {
        // Arrange & Act
        var invoiceResult = new InvoiceResult
        {
            InvoiceNumber = "INV-101",
            TaxAmount = 19.00m,
            TaxRate = 19.00m,
            Total = 119.00m,
            SubTotal = 100.00m,
            InvoiceDate = "2024-01-15",
            Merchant = null // This should be allowed
        };

        // Assert
        Assert.That(invoiceResult.Merchant, Is.Null);
    }

    [Test]
    public void InvoiceResult_ShouldExtractMerchantNameCorrectly()
    {
        // Arrange
        var merchant = new Merchant { Name = "ACME Corporation Ltd." };
        var invoiceResult = new InvoiceResult
        {
            InvoiceNumber = "INV-2024-001",
            TaxAmount = 19.00m,
            TaxRate = 19.00m,
            Total = 119.00m,
            SubTotal = 100.00m,
            InvoiceDate = "2024-01-15",
            Skonto = 2.50m,
            Merchant = merchant
        };

        // Act - Simulate what ParseService does
        var extractedVendorName = invoiceResult.Merchant?.Name;

        // Assert
        Assert.That(extractedVendorName, Is.EqualTo("ACME Corporation Ltd."));
        Assert.That(invoiceResult.Merchant.Name, Is.EqualTo("ACME Corporation Ltd."));
    }

    [Test]
    public void InvoiceResult_ShouldHandleEmptyMerchantName()
    {
        // Arrange
        var merchant = new Merchant { Name = "" };
        var invoiceResult = new InvoiceResult
        {
            InvoiceNumber = "INV-2024-002",
            TaxAmount = 19.00m,
            TaxRate = 19.00m,
            Total = 119.00m,
            SubTotal = 100.00m,
            InvoiceDate = "2024-01-15",
            Merchant = merchant
        };

        // Act - Simulate what ParseService does
        var extractedVendorName = invoiceResult.Merchant?.Name;

        // Assert
        Assert.That(extractedVendorName, Is.EqualTo(""));
        Assert.That(invoiceResult.Merchant.Name, Is.EqualTo(""));
    }
}