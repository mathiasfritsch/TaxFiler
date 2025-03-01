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
    public async Task<IEnumerable<DocumentDto>> List(DateOnly yearMonth)
    {
        return await documentService.GetDocumentsAsync(yearMonth);
    }

    [HttpGet("GetDocument/{documentId}")]
    public async Task<Result<DocumentDto>> GetDocument(int documentId)
    {
        return await documentService.GetDocumentAsync(documentId);
    }


    [HttpPost("AddDocument")]
    public async Task<Result<DocumentDto>> AddDocument(AddDocumentDto documentDto)
    {
        return await documentService.AddDocumentAsync(documentDto);
    }


    [HttpPost("UpdateDocument")]
    public async Task UpdateDocument(DocumentDto documentDto)
    {
        await documentService.UpdateDocumentAsync(documentDto.Id, documentDto);
    }


    [HttpPost("DeleteDocument/{id}")]
    public async Task DeleteDocument(int id)
    {
        await documentService.DeleteDocumentAsync(id);
    }
}