using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Mapper;
using TaxFiler.Model.Dto;
using TaxFiler.Models;
using TaxFiler.Service;

namespace TaxFiler.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet("GetTransactions")]
    public async Task<IEnumerable<TransactionViewModel>> List(string yearMonth)
    {
        var transactions = await transactionService.GetTransactionsAsync(Common.GetYearMonth(yearMonth));
        var vm = transactions.Select(t => t.ToViewModel());
        return vm;
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
    public async Task Upload(IFormFile file, string yearMonth)
    {
        var yearMonthDate = Common.GetYearMonth(yearMonth);
        var reader = new StreamReader(file.OpenReadStream());
        var transactions = transactionService.ParseTransactions(reader);
        await transactionService.AddTransactionsAsync(transactions, yearMonthDate);
    }

    public async Task DeleteTransaction(string yearMonth)
    {
        await transactionService.DeleteTransactionsAsync(Common.GetYearMonth(yearMonth));
    }

    public async Task DeleteTransactions(DateTime yearMonth)
    {
        await transactionService.DeleteTransactionsAsync(yearMonth);
    }


    [HttpGet("GetTransaction")]
    public async Task<TransactionDto> GetTransaction(int transactionId)
    {
        var transaction = await transactionService.GetTransactionAsync(transactionId);
        return transaction;
    }

    [HttpPost("UpdateTransaction")]
    public async Task UpdateTransaction(TransactionDto transactionDto)
    {
        await transactionService.UpdateTransactionAsync(transactionDto);
    }
}