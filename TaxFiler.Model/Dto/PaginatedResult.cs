namespace TaxFiler.Model.Dto;

/// <summary>
/// Generic paginated result container for API responses.
/// </summary>
/// <typeparam name="T">The type of items in the paginated result</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();
    
    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    
    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
    
    /// <summary>
    /// Creates a new paginated result.
    /// </summary>
    /// <param name="items">The items for the current page</param>
    /// <param name="pageNumber">Current page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="totalCount">Total number of items across all pages</param>
    public PaginatedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
    
    /// <summary>
    /// Creates an empty paginated result.
    /// </summary>
    public PaginatedResult()
    {
    }
}