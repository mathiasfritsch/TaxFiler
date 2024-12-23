using FluentResults;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.DB.Model;
using TaxFiler.Model.Dto;

namespace TaxFiler.Service;

public class DocumenService(TaxFilerContext context):IDocumentService
{
    public async Task DeleteAllDocumentsAsync()
    {
        context.Documents.RemoveRange(context.Documents);
        await context.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<DocumentDto>> GetDocumentsAsync() =>
        await context.Documents.Select(d => new DocumentDto
        {
            Id = d.Id,
            Name = d.Name,
            ExternalRef = d.ExternalRef,
            Orphaned = d.Orphaned,
            TaxRate = d.TaxRate,
            TaxAmount = d.TaxAmount,
            Total = d.Total,
            SubTotal = d.SubTotal,
            InvoiceDate = d.InvoiceDate,
            InvoiceNumber = d.InvoiceNumber,
            Parsed = d.Parsed
        }).ToArrayAsync();
    
    public async Task<Result<DocumentDto>> GetDocumentAsync(int id)
    {
        var document = await context.Documents.FindAsync(id);
        if(document == null)
        {
            return Result.Fail($"DocumentId {id} not found");
        }
        return Result.Ok(new DocumentDto
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
             Parsed = document.Parsed
        });
    }
    
    public async Task<Result<DocumentDto>> AddDocumentAsync(AddDocumentDto documentDto)
    {
        if (documentDto.Name == null)
        {
            return Result.Fail("Name is required");
        }
        if(documentDto.ExternalRef == null)
        {
            return Result.Fail("ExternalRef is required");
        }
        
        var document = new Document
        {
            Name = documentDto.Name,
            ExternalRef = documentDto.ExternalRef,
            Orphaned = documentDto.Orphaned,
            TaxRate = documentDto.TaxRate,
            TaxAmount = documentDto.TaxAmount,
            Total = documentDto.Total,
            SubTotal = documentDto.SubTotal,
            InvoiceDate = documentDto.InvoiceDate,
            InvoiceNumber = documentDto.InvoiceNumber,
            Parsed = documentDto.Parsed
        };
        
        await context.Documents.AddAsync(document);
        await context.SaveChangesAsync();
        return Result.Ok(new DocumentDto
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
            Parsed = document.Parsed
        });
    }
    
    public async Task<Result> UpdateDocumentAsync(int id, DocumentDto documentDto)
    {
        var document = await context.Documents.FindAsync(id);
        
        if(document == null)
        {
            return Result.Fail($"DocumentId {id} not found");
        }
        
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
        
        context.Entry(document).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return Result.Ok();
    }
    
    public async Task<Result> DeleteDocumentAsync(int id)
    {
        var document = await context.Documents.FindAsync(id);
        
        if(document == null)
        {
            return Result.Fail($"DocumentId {id} not found");
        }

        context.Remove(document);
        await context.SaveChangesAsync();
        return Result.Ok();
    }
    
}