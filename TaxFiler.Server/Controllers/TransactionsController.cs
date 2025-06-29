using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Mapper;
using TaxFiler.Model.Dto;
using TaxFiler.Models;
using TaxFiler.Service;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet("GetTransactions")]
    public async Task<IEnumerable<TransactionViewModel>> List(DateOnly yearMonth, [FromQuery] int? accountId = null)
    {
        var transactions = await transactionService.GetTransactionsAsync(yearMonth, accountId);
        var vm = transactions.Select(t => t.ToViewModel());
        return vm;
    }

    [HttpGet("Download")]
    public async Task<FileResult> Download(DateOnly yearMonth)
    {
        var memoryStream = await transactionService.CreateCsvFileAsync(yearMonth);

        var fileName = $"transactions_{yearMonth:yyyy-MM}.csv";
        memoryStream.Position = 0;

        return File(memoryStream, "text/csv", fileName);
    }

    [HttpPost("Upload")]
    public async Task Upload([FromForm]IFormFile file)
    {
        var reader = new StreamReader(file.OpenReadStream());
        var transactions = transactionService.ParseTransactions(reader);

        await transactionService.AddTransactionsAsync(transactions);
    }
    
    [HttpDelete("DeleteTransaction/{id:int}")]
    public async Task DeleteTransaction(int id)
    {
        await transactionService.DeleteTransactionAsync(id);
    }

    
    [HttpGet("GetTransaction/{id:int}")]
    public async Task<TransactionDto> GetTransaction(int id)
    {
        return await transactionService.GetTransactionAsync(id);
    }

    [HttpPost("UpdateTransaction")]
    public async Task UpdateTransaction(UpdateTransactionDto updateTransactionDto)
    {
        await transactionService.UpdateTransactionAsync(updateTransactionDto);
    }
}