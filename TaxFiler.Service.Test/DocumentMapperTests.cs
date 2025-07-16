using NUnit.Framework;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class DocumentMapperTests
{
    [Test]
    public void ToDto_ShouldMapVendorNameCorrectly()
    {
        // Arrange
        var document = new Document(
            name: "Test Document",
            externalRef: "ext123",
            orphaned: false,
            taxRate: 19.0m,
            taxAmount: 19.0m,
            total: 119.0m,
            subTotal: 100.0m,
            invoiceDate: DateOnly.Parse("2024-01-15"),
            invoiceNumber: "INV-123",
            parsed: true,
            skonto: 3.0m,
            vendorName: "Test Vendor GmbH"
        );

        // Act
        var dto = document.ToDto([]);

        // Assert
        Assert.That(dto.VendorName, Is.EqualTo("Test Vendor GmbH"));
        Assert.That(dto.Name, Is.EqualTo("Test Document"));
        Assert.That(dto.InvoiceNumber, Is.EqualTo("INV-123"));
    }

    [Test]
    public void ToDocument_ShouldMapVendorNameCorrectly()
    {
        // Arrange
        var addDocumentDto = new AddDocumentDto
        {
            Name = "Test Document",
            ExternalRef = "ext123",
            Orphaned = false,
            TaxRate = 19.0m,
            TaxAmount = 19.0m,
            Total = 119.0m,
            SubTotal = 100.0m,
            InvoiceDate = DateOnly.Parse("2024-01-15"),
            InvoiceNumber = "INV-123",
            Parsed = true,
            Skonto = 3.0m,
            VendorName = "Test Vendor GmbH"
        };

        // Act
        var document = addDocumentDto.ToDocument();

        // Assert
        Assert.That(document.VendorName, Is.EqualTo("Test Vendor GmbH"));
        Assert.That(document.Name, Is.EqualTo("Test Document"));
        Assert.That(document.InvoiceNumber, Is.EqualTo("INV-123"));
    }

    [Test]
    public void UpdateDocument_ShouldMapVendorNameCorrectly()
    {
        // Arrange
        var document = new Document(
            name: "Old Name",
            externalRef: "ext123",
            orphaned: false,
            taxRate: 19.0m,
            taxAmount: 19.0m,
            total: 119.0m,
            subTotal: 100.0m,
            invoiceDate: DateOnly.Parse("2024-01-15"),
            invoiceNumber: "INV-123",
            parsed: true,
            skonto: 3.0m,
            vendorName: "Old Vendor"
        );

        var updateDto = new UpdateDocumentDto
        {
            Name = "Updated Name",
            VendorName = "Updated Vendor GmbH",
            TaxRate = 20.0m,
            TaxAmount = 20.0m,
            Total = 120.0m,
            SubTotal = 100.0m,
            InvoiceNumber = "INV-456",
            Parsed = true,
            Skonto = 2.0m
        };

        // Act
        document.UpdateDocument(updateDto);

        // Assert
        Assert.That(document.VendorName, Is.EqualTo("Updated Vendor GmbH"));
        Assert.That(document.Name, Is.EqualTo("Updated Name"));
        Assert.That(document.InvoiceNumber, Is.EqualTo("INV-456"));
    }

    [Test]
    public void ToDto_ShouldHandleNullVendorName()
    {
        // Arrange
        var document = new Document(
            name: "Test Document",
            externalRef: "ext123",
            orphaned: false,
            taxRate: 19.0m,
            taxAmount: 19.0m,
            total: 119.0m,
            subTotal: 100.0m,
            invoiceDate: DateOnly.Parse("2024-01-15"),
            invoiceNumber: "INV-123",
            parsed: true,
            skonto: 3.0m,
            vendorName: null
        );

        // Act
        var dto = document.ToDto([]);

        // Assert
        Assert.That(dto.VendorName, Is.Null);
    }
}