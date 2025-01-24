using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Controllers;

[Authorize]
[Route("api/documents")]
[ApiController]
public class TaxFilerApi : ControllerBase
{
    private readonly IDocumentService _documentService;

    public TaxFilerApi(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments(string yearMonth)
    {
        var date = Common.GetYearMonth(yearMonth);
        var documents = await _documentService.GetDocumentsAsync(new DateOnly(date.Year, date.Month, 1));
        return Ok(documents);
    }

}