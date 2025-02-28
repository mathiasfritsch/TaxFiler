using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class DocumentsController(IDocumentService documentService) : ControllerBase
{

    [HttpGet("")]
    [HttpGet("List")]
    public async Task<IEnumerable<DocumentDto>> List(string yearMonth)
    {
        var date = Common.GetYearMonth(yearMonth);
        return await documentService.GetDocumentsAsync(new DateOnly(date.Year, date.Month, 1));
    }
    
    [HttpGet("GeDocument/{documentId}")]
    public async Task<Result<DocumentDto>> GetDocument(int documentId)
    {
        var result = await documentService.GetDocumentAsync(documentId);
        return result.Value;
    }
    
    
    [HttpPost("AddDocument")]
    public async Task<DocumentDto> AddDocument(AddDocumentDto documentDto)
    { 
        var result = await documentService.AddDocumentAsync( documentDto);
        return result.Value;
    }
    
    [HttpPost("UpdateDocument")]
    public async Task UpdateDocument( DocumentDto documentDto)
    {
        var result = await documentService.UpdateDocumentAsync(documentDto.Id, documentDto);
    }
    
    [HttpPost("DeleteDocument/{id}")]
    public async Task DeleteDocument(int id)
    {
        var result = await documentService.DeleteDocumentAsync(id);
    }
}