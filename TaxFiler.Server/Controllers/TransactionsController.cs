using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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

    [HttpGet("DownloadExcel")]
    public async Task<FileResult> DownloadExcel(DateOnly yearMonth)
    {
        var memoryStream = await transactionService.CreateExcelFileAsync(yearMonth);

        var fileName = $"Umsätze-{yearMonth:MM-yyyy}.xlsx";

        return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
    
    
    [SwaggerIgnore]
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

    /// <summary>
    /// Auto-assigns documents to unmatched transactions in a given month.
    /// </summary>
    /// <param name="yearMonth">The year-month to process (format: yyyy-MM-dd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary of auto-assignment results</returns>
    [HttpPost("auto-assign")]
    [ProducesResponseType(typeof(AutoAssignResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AutoAssignResult>> AutoAssignDocuments(
        [FromQuery] DateOnly yearMonth,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await transactionService.AutoAssignDocumentsAsync(
                yearMonth, 
                cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError, 
                $"An error occurred during auto-assignment: {ex.Message}");
        }
    }

    /// <summary>
    /// Lists the sequence-ordered documents linked to a transaction.
    /// </summary>
    [HttpGet("{transactionId:int}/documents")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TransactionDocumentDto>>> GetDocuments(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        var documents = await transactionService.GetTransactionDocumentsAsync(transactionId, cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Links a document to a transaction (many-to-many). A document may be linked
    /// to several transactions and vice versa.
    /// </summary>
    [HttpPost("{transactionId:int}/documents")]
    [ProducesResponseType(typeof(TransactionDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionDocumentDto>> AddDocument(
        int transactionId,
        [FromBody] AddDocumentToTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await transactionService.AddDocumentToTransactionAsync(transactionId, dto, cancellationToken);

        if (result.IsFailed)
        {
            var message = string.Join("; ", result.Errors.Select(e => e.Message));
            var isNotFound = message.Contains("not found", StringComparison.OrdinalIgnoreCase);
            return isNotFound
                ? NotFound(message)
                : Conflict(message);
        }

        return CreatedAtAction(nameof(GetDocuments), new { transactionId }, result.Value);
    }

    /// <summary>
    /// Removes a document link from a transaction. The document and transaction
    /// records themselves are left intact.
    /// </summary>
    [HttpDelete("{transactionId:int}/documents/{documentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveDocument(
        int transactionId,
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var result = await transactionService.RemoveDocumentFromTransactionAsync(transactionId, documentId, cancellationToken);

        if (result.IsFailed)
        {
            return NotFound(string.Join("; ", result.Errors.Select(e => e.Message)));
        }

        return NoContent();
    }
}
