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
    public async Task<ActionResult<IEnumerable<DocumentDto>>> Index()
    {
        return View( await documentService.GetDocumentsAsync());
    }
    
    [HttpGet("EditDocument/{documentId}")]
    public async Task<ActionResult<DocumentDto>> EditDocument(int documentId)
    {
        var result = await documentService.GetDocumentAsync(documentId);
        
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("Index");
        }
        ViewBag.Document = result.Value;
        return View(result.Value);
    }
    
        
    [HttpGet("AddDocument")]
    public ActionResult AddDocument()
    {
        return View();
    }
    
    [HttpPost("AddDocument")]
    public async Task<ActionResult<DocumentDto>> AddDocument(AddDocumentDto documentDto)
    {
        var result = await documentService.AddDocumentAsync( documentDto);
        
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("EditDocument");
        }
        
        return RedirectToAction("Index", "Home");
    }
    
    [HttpPost("UpdateDocument")]
    public async Task<IActionResult> UpdateDocument( DocumentDto documentDto)
    {
        var result = await documentService.UpdateDocumentAsync(documentDto.Id, documentDto);
        
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("EditDocument");
        }
        
        return RedirectToAction("Index", "Home");
    }
    
    [HttpPost("DeleteDocument/{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var result = await documentService.DeleteDocumentAsync(id);
        // ReSharper disable once InvertIf
        if (result.IsFailed)
        {
            TempData["Error"] = result.Errors.First().Message;
            RedirectToAction("EditDocument");
        }
        
        return RedirectToAction("Index", "Home");
    }
}