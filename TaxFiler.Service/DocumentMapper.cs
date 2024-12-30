using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public static class DocumentMapper
{
    public static DocumentDto ToDto(this Document document) =>
        new()
        {
            Id = document.Id,
            Name = document.Name,
            ExternalRef = document.ExternalRef,
            Orphaned = document.Orphaned,
            TaxRate = document.TaxRate,
            TaxAmount = document.TaxAmount,
            Total = document.Total,
            SubTotal = document.SubTotal,
            InvoiceDate = document.InvoiceDate,
            InvoiceNumber = document.InvoiceNumber,
            Parsed = document.Parsed,
            Skonto = document.Skonto
        };

    public static Document ToDocument(this AddDocumentDto addDocumentDto)
    {
        return new Document(name: addDocumentDto.Name, 
            externalRef: addDocumentDto.ExternalRef,
            orphaned: addDocumentDto.Orphaned, 
            taxRate: addDocumentDto.TaxRate, 
            taxAmount: addDocumentDto.TaxAmount,
            total: addDocumentDto.Total, 
            subTotal: addDocumentDto.SubTotal, 
            invoiceDate: addDocumentDto.InvoiceDate,
            invoiceNumber: addDocumentDto.InvoiceNumber, 
            parsed: addDocumentDto.Parsed,
            skonto: addDocumentDto.Skonto);
    }
    
    public static void UpdateDocument(this Document document, DocumentDto documentDto)
    {
        document.Name = documentDto.Name;
        document.ExternalRef = documentDto.ExternalRef;
        document.Orphaned = documentDto.Orphaned;
        document.TaxRate = documentDto.TaxRate;
        document.TaxAmount = documentDto.TaxAmount;
        document.Total = documentDto.Total;
        document.SubTotal = documentDto.SubTotal;
        document.InvoiceDate = documentDto.InvoiceDate;
        document.InvoiceNumber = documentDto.InvoiceNumber;
        document.Parsed = documentDto.Parsed;
        document.Skonto = documentDto.Skonto;
    }
}