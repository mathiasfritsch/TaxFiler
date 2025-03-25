using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.Model.Llama;
using TaxFiler.Service;
using FileData = TaxFiler.Model.FileData;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HomeController(
    ITransactionService transactionService,
    TaxFilerContext taxFilerContext,
    ISyncService syncService,
    IGoogleDriveService googleDriveService,
    IParseService parseService,
    IDocumentService documentService) : ControllerBase
{
    [HttpGet("TestDB")]
    public string TestDb()
    {
        try
        {
            taxFilerContext.Database.OpenConnection();
            taxFilerContext.Database.CloseConnection();
        }
        catch (Exception e)
        {
            return e.ToString();
        }

        return "ok";
    }

    [HttpPost("SyncFiles")]
    public async Task SyncFiles(DateOnly yearMonth)
    {
        await syncService.SyncFilesAsync(yearMonth);
    }


    [HttpDelete("DeleteAll")]
    public async Task DeleteAllDocuments()
    {
        await documentService.DeleteAllDocumentsAsync();
    }

    [HttpDelete("GoogleDocuments")]
    public async Task<List<FileData>> GoogleDocuments(DateOnly yearMonth)
    {
        return await googleDriveService.GetFilesAsync(yearMonth);
    }


}