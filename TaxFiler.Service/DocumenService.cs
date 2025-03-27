﻿using FluentResults;
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
        await context.Documents
            .Select(d => d.ToDto())
            .ToArrayAsync();
    
    
    
    public async Task<Result<DocumentDto>> GetDocumentAsync(int id)
    {
        var document = await context.Documents.FindAsync(id);
        if(document == null)
        {
            return Result.Fail($"DocumentId {id} not found");
        }
        return Result.Ok(document.ToDto());
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
        
        return Result.Ok(document.ToDto());
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

    public async Task<IEnumerable<DocumentDto>> GetDocumentsByMonthAsync(DateTime yearMonth)
     => await context.Documents
         .Select(d => d.ToDto())
        .ToArrayAsync();

}