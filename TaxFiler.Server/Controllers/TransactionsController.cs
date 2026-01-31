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
public class TransactionsController(
    ITransactionService transactionService,
    IDocumentMatchingService matchingService) : ControllerBase
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
    /// Enhanced to support multiple document attachments per transaction.
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
    /// Auto-assigns multiple documents to transactions in a given month using enhanced matching logic.
    /// This endpoint specifically targets transactions that can benefit from multiple document attachments.
    /// </summary>
    /// <param name="yearMonth">The year-month to process (format: yyyy-MM-dd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary of multiple document assignment results</returns>
    [HttpPost("auto-assign-multiple")]
    [ProducesResponseType(typeof(IEnumerable<MultipleAssignmentResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MultipleAssignmentResult>>> AutoAssignMultipleDocuments(
        [FromQuery] DateOnly yearMonth,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all transactions for the specified month
            var transactions = await transactionService.GetTransactionsAsync(yearMonth);
            var results = new List<MultipleAssignmentResult>();

            // Process each transaction for multiple document assignment
            foreach (var transaction in transactions)
            {
                var assignmentResult = await matchingService.AutoAssignMultipleDocumentsAsync(
                    transaction.Id, 
                    cancellationToken);

                if (assignmentResult.IsSuccess && assignmentResult.Value.DocumentsAttached > 0)
                {
                    results.Add(assignmentResult.Value);
                }
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError, 
                $"An error occurred during multiple document auto-assignment: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes multiple transactions for bulk document assignment operations.
    /// Useful for batch processing scenarios where multiple transactions need document attachments.
    /// </summary>
    /// <param name="transactionIds">List of transaction IDs to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping transaction IDs to their assignment results</returns>
    [HttpPost("bulk-auto-assign")]
    [ProducesResponseType(typeof(Dictionary<int, MultipleAssignmentResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<int, MultipleAssignmentResult>>> BulkAutoAssignDocuments(
        [FromBody] IEnumerable<int> transactionIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!transactionIds.Any())
            {
                return BadRequest("No transaction IDs provided");
            }

            var results = new Dictionary<int, MultipleAssignmentResult>();

            // Process each transaction
            foreach (var transactionId in transactionIds)
            {
                var assignmentResult = await matchingService.AutoAssignMultipleDocumentsAsync(
                    transactionId, 
                    cancellationToken);

                if (assignmentResult.IsSuccess)
                {
                    results[transactionId] = assignmentResult.Value;
                }
                else
                {
                    // Create a result indicating failure
                    results[transactionId] = new MultipleAssignmentResult
                    {
                        TransactionId = transactionId,
                        DocumentsAttached = 0,
                        TotalAmount = 0,
                        HasWarnings = true,
                        Warnings = assignmentResult.Errors.Select(e => e.Message),
                        AttachedDocuments = Enumerable.Empty<DocumentDto>()
                    };
                }
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError, 
                $"An error occurred during bulk auto-assignment: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets multiple document match suggestions for a specific transaction.
    /// Useful for showing users potential document combinations before auto-assignment.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to get matches for</param>
    /// <param name="unconnectedOnly">Whether to only return unconnected documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of multiple document match combinations</returns>
    [HttpGet("{transactionId}/multiple-document-matches")]
    [ProducesResponseType(typeof(IEnumerable<MultipleDocumentMatch>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MultipleDocumentMatch>>> GetMultipleDocumentMatches(
        int transactionId,
        [FromQuery] bool unconnectedOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var matches = await matchingService.FindMultipleDocumentMatchesAsync(
                transactionId, 
                unconnectedOnly, 
                cancellationToken);

            return Ok(matches);
        }
        catch (ArgumentException)
        {
            return NotFound($"Transaction with ID {transactionId} not found");
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError, 
                $"An error occurred while finding document matches: {ex.Message}");
        }
    }
}