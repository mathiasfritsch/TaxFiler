namespace TaxFiler.Service.DocumentMatcher;

public class MatchingFeatures
{
    public float AmountSimilarity { get; set; }
    
    public float DateDiffDays { get; set; }

    public float VendorSimilarity { get; set; }

    public float InvoiceNumberMatch { get; set; }
    
    public float SkontoMatch { get; set; }
    
    public float TaxRateMatch { get; set; }
    public float PatternMatch { get; set; }
    
    public bool IsMatch { get; set; }
    
    // Additional properties for analysis
    public int DocumentId { get; set; }
    public int TransactionId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string TransactionNote { get; set; } = string.Empty;
}
