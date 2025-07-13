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
}