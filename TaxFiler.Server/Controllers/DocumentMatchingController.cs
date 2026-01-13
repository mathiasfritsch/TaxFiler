using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxFiler.Service;

namespace TaxFiler.Server.Controllers;

/// <summary>
/// Controller for document matching operations.
/// Provides endpoints for finding and ranking documents that match financial transactions.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentMatchingController : ControllerBase
{
    private readonly IDocumentMatchingService _documentMatchingService;

    /// <summary>
    /// Initializes a new instance of the DocumentMatchingController.
    /// </summary>
    /// <param name="documentMatchingService">Service for document matching operations</param>
    public DocumentMatchingController(IDocumentMatchingService documentMatchingService)
    {
        _documentMatchingService = documentMatchingService ?? throw new ArgumentNullException(nameof(documentMatchingService));
    }

    /// <summary>
    /// Finds and ranks matching documents for a specific transaction.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction to find matches for</param>
    /// <param name="unconnectedOnly">If true, only return documents not already connected to transactions (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ranked list of document matches ordered by confidence score (highest first)</returns>
    /// <response code="200">Returns the list of matching documents</response>
    /// <response code="404">Transaction not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("matches/{transactionId:int}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentMatch>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentMatch>>> GetDocumentMatches(
        int transactionId,
        [FromQuery] bool unconnectedOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var matches = await _documentMatchingService.DocumentMatchesAsync(transactionId, unconnectedOnly, cancellationToken);
            return Ok(matches);
        }
        catch (ArgumentException ex)
        {
            return NotFound($"Transaction with ID {transactionId} not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                $"An error occurred while finding document matches: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs batch document matching for multiple transactions.
    /// </summary>
    /// <param name="transactionIds">Array of transaction IDs to find matches for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping transaction IDs to their document matches</returns>
    /// <response code="200">Returns the batch matching results</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("batch-matches")]
    [ProducesResponseType(typeof(Dictionary<int, IEnumerable<DocumentMatch>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<int, IEnumerable<DocumentMatch>>>> GetBatchDocumentMatches(
        [FromBody] int[] transactionIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (transactionIds == null || transactionIds.Length == 0)
            {
                return BadRequest("Transaction IDs array cannot be null or empty");
            }

            if (transactionIds.Length > 100)
            {
                return BadRequest("Maximum 100 transactions can be processed in a single batch request");
            }

            // Get transactions from database
            var transactions = new List<DB.Model.Transaction>();
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DB.TaxFilerContext>();
            
            foreach (var id in transactionIds)
            {
                var transaction = await context.Transactions.FindAsync(new object[] { id }, cancellationToken);
                if (transaction != null)
                {
                    transactions.Add(transaction);
                }
            }

            var matches = await _documentMatchingService.BatchDocumentMatchesAsync(transactions, cancellationToken);
            return Ok(matches);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                $"An error occurred while performing batch document matching: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current matching configuration settings.
    /// </summary>
    /// <returns>Current matching configuration</returns>
    /// <response code="200">Returns the current matching configuration</response>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(MatchingConfiguration), StatusCodes.Status200OK)]
    public ActionResult<MatchingConfiguration> GetConfiguration()
    {
        var config = HttpContext.RequestServices.GetRequiredService<MatchingConfiguration>();
        return Ok(config);
    }
}