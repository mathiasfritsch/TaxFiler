using Microsoft.AspNetCore.Mvc;
using TaxFiler.DB;
using TaxFiler.DB.Model;

namespace TaxFilerApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{

    private readonly TaxFilerContext _taxFilerContext;

    public TestController(TaxFilerContext taxFilerContext)
    {
        _taxFilerContext = taxFilerContext;
    }

    [HttpGet(Name = "Test")]
    public string Get()
    {
        _taxFilerContext.Documents.Add(
            new Document
            {
                ExternalRef = "external",
                Name = "name",
                Orphaned = false
            }

        );
        _taxFilerContext.SaveChanges();

        var doc1 = _taxFilerContext.Documents.First();
        
        return doc1.Id.ToString();
    }
}