using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DBController(
    TaxFilerContext taxFilerContext) : ControllerBase
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

    private static string GetSome(string? value)
    {
        if(value?.Length > 5)
        {
            return value.Substring(0, 5);
        }
        return value ??"";
    }
    
    [HttpGet("EnvironmentVariables")]
    public IActionResult GetEnvironmentVariables()
    {
        try
        {
            var environmentVariables = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .Select(entry => new
                {
                    Name = entry.Key?.ToString() ?? string.Empty,
                    HasValue = !string.IsNullOrEmpty(entry.Value?.ToString()),
                    ValueLength = entry.Value?.ToString()?.Length ?? 0,
                    ValueType = entry.Value?.GetType().Name ?? "null",
                    ValueSome =  GetSome(entry.Value?.ToString())
                })
                .OrderBy(env => env.Name)
                .ToList();

            return Ok(new
            {
                Count = environmentVariables.Count,
                Variables = environmentVariables
            });
        }
        catch (Exception e)
        {
            return BadRequest(new
            {
                Error = "Failed to retrieve environment variables",
                Message = e.Message
            });
        }
    }

}