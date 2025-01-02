using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaxFiler.Mapper;
using TaxFiler.Model.Dto;
using TaxFiler.Models;
using TaxFiler.Service;

namespace TaxFiler.Controllers;

[Authorize]
public class TransactionsController(ITransactionService transactionService, 
    IDocumentService documentService) : Controller
{
 
    
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<ActionResult<IEnumerable<TransactionViewModel>>> IndexAsync(string yearMonth)
    {
        ViewBag.YearMonth = yearMonth;
        
        var transactions = await transactionService.GetTransactionsAsync(Common.GetYearMonth(yearMonth));
        var vm = transactions.Select(t => t.ToViewModel());
        return View( vm);
    }
    
    [HttpGet("Download")]
    public async Task<FileResult> Download(string yearMonth)
    {
        var yearMonthDate = Common.GetYearMonth(yearMonth);
        var memoryStream = await transactionService.CreateCsvFileAsync(yearMonthDate);
    
        var fileName = $"transactions_{yearMonthDate:yyyy-MM}.csv";
        memoryStream.Position = 0;
    
        return File(memoryStream, "text/csv", fileName);
    }
    
    [HttpPost("Upload")]
    public async Task<IActionResult> Upload(IFormFile file,string yearMonth)
    {
        var yearMonthDate = Common.GetYearMonth(yearMonth);
        
        if(file.Length > 0)
        {
            try
            {
                var reader = new StreamReader(file.OpenReadStream());
                var transactions = transactionService.ParseTransactions(reader);
                
                await transactionService.AddTransactionsAsync(transactions, yearMonthDate);
                    
                ViewBag.Message = "File processed successfully!";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"ERROR: {ex.Message}";
            }
        }
        else
        {
            ViewBag.Message = "No file selected!";
        }
        
        TempData["Message"] = "File uploaded and processed successfully.";
        
        return RedirectToAction("Index", new { yearMonth });
    }

    public async Task<IActionResult> DeleteTransaction(string yearMonth)
    {
        await transactionService.DeleteTransactionsAsync(Common.GetYearMonth(yearMonth));
        ViewBag.YearMonth = yearMonth;
        
        return RedirectToAction("Index", new { yearMonth });
    }
    
    public async Task<IActionResult> DeleteTransactions(string yearMonth)
    {
        await transactionService.DeleteTransactionsAsync(Common.GetYearMonth(yearMonth));
        ViewBag.YearMonth = yearMonth;
        
        return RedirectToAction("Index", new { yearMonth });
    }
    
    [HttpGet("EditTransaction")]
    public async Task<IActionResult> EditTransaction(string yearMonth, int transactionId)
    {
        
        var transaction = await transactionService.GetTransactionAsync(transactionId);
        var documents = await documentService.GetDocumentsByMonthAsync(Common.GetYearMonth(yearMonth));

        ViewBag.YearMonth = yearMonth;
        ViewBag.Documents = documents.Select( s=> new SelectListItem(s.Name,  s.Id.ToString(), s.Id == transaction.DocumentId));
        return View(transaction);
    }
    
    [HttpPost("UpdateTransaction")]
    public async Task<IActionResult> UpdateTransaction(string yearMonth, Model.Dto.TransactionDto transactionDto)
    {
        await transactionService.UpdateTransactionAsync(transactionDto);
        ViewBag.YearMonth = yearMonth;
        return RedirectToAction("Index", new { yearMonth });
    }
}