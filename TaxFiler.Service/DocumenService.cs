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

    async Task<IEnumerable<DocumentDto>> IDocumentService.GetDocumentsAsync(DateOnly? yearMonth = null)
    {
        var documentsWithTransactions = context.Transactions
            .Where(t => t.DocumentId != null)
            .Select( t => t.DocumentId)
            .Distinct()
            .ToArray();
        
        var query = context.Documents.AsQueryable();
        
        if (yearMonth.HasValue)
        {
            query = query.Where(d => d.InvoiceDateFromFolder.HasValue && 
                                     d.InvoiceDateFromFolder.Value.Year == yearMonth.Value.Year &&
                                     d.InvoiceDateFromFolder.Value.Month == yearMonth.Value.Month);
        }
        
        return await query
            .Select(d => d.ToDto(documentsWithTransactions))
            .ToArrayAsync();
    }


    public async Task<Result<DocumentDto>> GetDocumentAsync(int id)
    {
        var document = await context.Documents.FindAsync(id);
        if(document == null)
        {
            return Result.Fail($"DocumentId {id} not found");
        }
        return Result.Ok(document.ToDto([]));
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
        
        var document = documentDto.ToDocument();
        
        await context.Documents.AddAsync(document);
        await context.SaveChangesAsync();
        
        return Result.Ok(document.ToDto([]));
    }
    
    public async Task<Result> UpdateDocumentAsync(int id, UpdateDocumentDto documentDto)
    {
        var document = await context.Documents.FindAsync(id);
        
        if(document == null)
        {
            return Result.Fail($"DocumentId {id} not found");
        }
        
        document.UpdateDocument(documentDto);
        
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

        var transactionsWithDocument = context.Transactions.Where(t => t.DocumentId == id);

        foreach (var transaction in transactionsWithDocument)
        {
            transaction.Document = null;
        }
        
        context.Remove(document);
        await context.SaveChangesAsync();
        return Result.Ok();
    }
    
}