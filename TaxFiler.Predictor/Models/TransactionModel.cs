namespace TaxFiler.Predictor.Models;

public class TransactionModel
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public string? Counterparty { get; set; }
    public string? TransactionReference { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public string? TransactionNote { get; set; }
    public bool? IsOutgoing { get; set; }
    public bool? IsIncomeTaxRelevant { get; set; }
    public bool? IsSalesTaxRelevant { get; set; }
    public int? TaxMonth { get; set; }
    public int? TaxYear { get; set; }
    public int? DocumentId { get; set; }
    public string? SenderReceiver { get; set; }
    
    // Helper properties for generic text processing
    public string TransactionText => $"{SenderReceiver} {TransactionNote}".ToLowerInvariant();
    public List<string> ExtractedNumbers => ExtractNumbers();
    public List<string> ExtractedWords => ExtractWords();
    
    public List<string> ExtractNumbers()
    {
        var numbers = new List<string>();
        var text = $"{TransactionNote} {SenderReceiver}";
        if (string.IsNullOrEmpty(text)) return numbers;
        
        var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\d+");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Value.Length >= 3) // Only include numbers with 3+ digits
                numbers.Add(match.Value);
        }
        return numbers;
    }
    
    private List<string> ExtractWords()
    {
        var text = $"{SenderReceiver} {TransactionNote}".ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.Split(text, @"[^a-zA-Z]+")
            .Where(word => word.Length > 2)
            .ToList();
    }
}