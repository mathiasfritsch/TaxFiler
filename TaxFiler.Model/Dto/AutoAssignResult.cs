namespace TaxFiler.Model.Dto;

/// <summary>
/// Result summary from bulk auto-assignment operation.
/// </summary>
public class AutoAssignResult
{
    /// <summary>
    /// Total number of unmatched transactions processed.
    /// </summary>
    public int TotalProcessed { get; set; }
    
    /// <summary>
    /// Number of transactions successfully assigned a document.
    /// </summary>
    public int AssignedCount { get; set; }
    
    /// <summary>
    /// Number of transactions skipped (no match above threshold).
    /// </summary>
    public int SkippedCount { get; set; }
    
    /// <summary>
    /// List of error messages for transactions that failed to process.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
