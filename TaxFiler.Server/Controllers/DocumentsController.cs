using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refit;
using TaxFiler.Model.Dto;
using TaxFiler.Model.Llama;
using TaxFiler.Service;
using TaxFiler.Service.LlamaClient;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController(IDocumentService documentService, 
    IParseService parseService,
    ISyncService syncService,
    ILlamaApiClient llamaApiClient) : ControllerBase
{
    [HttpGet("UploadFileForParsing")]
    public async Task<string> UploadFileForParsing(CancellationToken cancellationToken)
    {
        // var filePath = "C:/documents/file.PDF";
        // await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        // var filePart = new StreamPart(fileStream, Path.GetFileName(filePath), "application/pdf");
        // await llamaApiClient.UploadFileForParsingAsync(filePart);
        string res = await llamaApiClient.GetAgents();
        
        return res;
    }
    
    [HttpGet("")]
    [HttpGet("GetDocuments")]
    public async Task<IEnumerable<DocumentDto>> List()
    {
        return await documentService.GetDocumentsAsync();
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
    public async Task UpdateDocument(UpdateDocumentDto documentDto)
    {
        await documentService.UpdateDocumentAsync(documentDto.Id, documentDto);
    }


    [HttpDelete("DeleteDocument/{id}")]
    public async Task DeleteDocument(int id)
    {
        await documentService.DeleteDocumentAsync(id);
    }
    
    [HttpPost("SyncFiles/{yearMonth}")]
    public async Task SyncFiles(DateOnly yearMonth)
    {
        await syncService.SyncFilesAsync(yearMonth);
    }
    
    [HttpPost("Parse/{documentId}")]
    public async Task<Result<Invoice>> Parse([FromRoute] int documentId)
    {
        return await parseService.ParseFilesAsync(documentId);
    }
}