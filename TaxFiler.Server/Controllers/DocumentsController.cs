using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Model.Dto;
using TaxFiler.Service;
using TaxFiler.Service.LlamaClient;
using TaxFiler.Service.LlamaIndex;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController(
    IDocumentService documentService,
    IParseService parseService,
    ISyncService syncService,
    ILlamaApiClient llamaApiClient,
    IGoogleDriveService googleDriveService) : ControllerBase
{
    [HttpGet("DownloadDocument/{documentId}")]
    public async Task<IActionResult> DownloadDocument(int documentId)
    {
        var document = await documentService.GetDocumentAsync(documentId);

        if (document.IsFailed || document.Value == null)
        {
            return NotFound("Document not found.");
        }

        var fileId = document.Value.ExternalRef;
        var fileBytes = await googleDriveService.DownloadFileAsync(fileId);

        if (fileBytes.Length == 0)
        {
            return NotFound("File not found on Google Drive.");
        }

        var fileName = document.Value.Name;
        var contentType = "application/pdf";

        return File(fileBytes, contentType, fileName, enableRangeProcessing: true);
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
    
    [HttpPost("SyncAllFoldersAsync")]
    public async Task SyncAllFoldersAsync()
    {
        await syncService.SyncAllFoldersAsync();
    }
    
    [HttpPost("SyncFiles/{yearMonth}")]
    public async Task SyncFiles(DateOnly yearMonth)
    {
        await syncService.SyncFilesAsync(yearMonth);
    }

    [HttpPost("SyncAllFolders")]
    public async Task<ActionResult> SyncAllFolders()
    {
        try
        {
            await syncService.SyncAllFoldersAsync();
            return Ok(new { Message = "All folders synced successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = "Failed to sync all folders",
                Message = ex.Message
            });
        }
    }

    [HttpPost("Parse/{documentId}")]
    public async Task<Result<LlamaIndexJobResultResponse>> Parse([FromRoute] int documentId)
    {
        return await parseService.ParseFilesAsync(documentId);
    }

    [HttpGet("FolderStructure")]
    public async Task<ActionResult<GoogleDriveFolderStructureDto>> GetFolderStructure()
    {
        try
        {
            var folderStructure = await googleDriveService.GetFolderStructureAsync();
            return Ok(folderStructure);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = "Failed to retrieve folder structure from Google Drive",
                Message = ex.Message
            });
        }
    }
}