using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Model.Dto;
using TaxFiler.Service;

namespace TaxFiler.Controllers;

[Authorize]
[Route("transactions")]
public class TransactionsController(ITransactionService transactionService) : Controller
{
 
    
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> IndexAsync()
    {
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
}