using Microsoft.ML.Data;

namespace TaxFiler.Predictor.Models;

public class MatchingFeatures
{
    [LoadColumn(0)]
    public float AmountSimilarity { get; set; }

    [LoadColumn(1)]
    public float DateDiffDays { get; set; }

    [LoadColumn(2)]
    public float VendorSimilarity { get; set; }

    [LoadColumn(3)]
    public float InvoiceNumberMatch { get; set; }

    [LoadColumn(4)]
    public float SkontoMatch { get; set; }

    [LoadColumn(5)]
    public float PatternMatch { get; set; }

    [LoadColumn(6)]
    public bool IsMatch { get; set; }
    
    // Additional properties for analysis
    public int DocumentId { get; set; }
    public int TransactionId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string TransactionNote { get; set; } = string.Empty;
}

public class MatchingPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsMatch { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
    
    [ColumnName("Score")]
    public float Score { get; set; }
}