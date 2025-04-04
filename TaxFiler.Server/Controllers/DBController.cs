using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.Service;
using FileData = TaxFiler.Model.FileData;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DBController(
    TaxFilerContext taxFilerContext,
    IGoogleDriveService googleDriveService,
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
    
    [HttpGet("RunMigrations")]
    public async Task<string> RunMigrations()
    {
        try
        {
            await taxFilerContext.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            return e.ToString();
        }

        return "ok";
    }
    
}