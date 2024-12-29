using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Controllers;

[Authorize]
public class TransactionsController(ITransactionService transactionService) : Controller
{
 
    
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> IndexAsync(string yearMonth)
    {
        ViewBag.YearMonth = yearMonth;
        return View( await transactionService.GetTransactionsAsync());
    }
    
    [HttpPost("Upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if(file.Length > 0)
        {
            try
            {
                var reader = new StreamReader(file.OpenReadStream());

                var transactions = transactionService.ParseTransactions(reader);
                await transactionService.AddTransactionsAsync(transactions);
                    
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
        
        return RedirectToAction("Index");
    }

    public IActionResult DeleteTransaction()
    {
        throw new NotImplementedException();
    }
    
    [HttpGet("EditTransaction")]
    public async Task<IActionResult> EditTransaction(string yearMonth, int transactionId)
    {
        var transaction = await transactionService.GetTransactionAsync(transactionId);
        ViewBag.YearMonth = yearMonth;
        return View(transaction);
    }
    
    [HttpPost("UpdateTransaction")]
    public async Task<IActionResult> UpdateTransaction(string yearMonth, Model.Dto.TransactionDto transactionDto)
    {
        await transactionService.UpdateTransactionAsync(transactionDto);
        ViewBag.YearMonth = yearMonth;
        return RedirectToAction("Index");
    }
}