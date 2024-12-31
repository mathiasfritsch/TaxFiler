using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Controllers;

[Authorize]
[Route("documents")]
public class DocumentsController(IDocumentService documentService) : Controller
{

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> Index(string yearMonth)
    {
        var date = Common.GetYearMonth(yearMonth);
        return View( await documentService.GetDocumentsAsync(new DateOnly(date.Year, date.Month, 1)));
    }
    
    [HttpGet("EditDocument/{documentId}")]
    public async Task<ActionResult<DocumentDto>> EditDocument(int documentId, string yearMonth )
    {
        var result = await documentService.GetDocumentAsync(documentId);
        
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("Index", "Home", new {yearMonth});
        }
        ViewBag.Document = result.Value;
        ViewBag.YearMonth = yearMonth;
        return View(result.Value);
    }
    
        
    [HttpGet("AddDocument")]
    public ActionResult AddDocument(string yearMonth)
    {
        ViewBag.YearMonth = yearMonth;
        
        return View();
    }
    
    [HttpPost("AddDocument")]
    public async Task<ActionResult<DocumentDto>> AddDocument(AddDocumentDto documentDto, string yearMonth)
    {
        var result = await documentService.AddDocumentAsync( documentDto);
        
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("EditDocument");
        }
        
        return RedirectToAction("Index", "Home", new {yearMonth});
    }
    
    [HttpPost("UpdateDocument")]
    public async Task<IActionResult> UpdateDocument( DocumentDto documentDto, string yearMonth)
    {
        var result = await documentService.UpdateDocumentAsync(documentDto.Id, documentDto);
        
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("EditDocument","Documents", new {documentDto.Id, yearMonth});
        }
        
        return RedirectToAction("Index", "Home", new {yearMonth});
    }
    
    [HttpPost("DeleteDocument/{id}")]
    public async Task<IActionResult> DeleteDocument(int id,string yearMonth)
    {
        var result = await documentService.DeleteDocumentAsync(id);
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("EditDocument");
        }
        
        return RedirectToAction("Index", "Home", new {yearMonth});
    }
}