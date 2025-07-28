namespace TaxFiler.Predictor.Models;

public class DocumentModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExternalRef { get; set; } = string.Empty;
    public bool Orphaned { get; set; }
    public bool Parsed { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? Total { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Skonto { get; set; }
    public string? VendorName { get; set; }
    public DateTime? InvoiceDateFromFolder { get; set; }
    
    // Helper properties for generic text processing
    public string DocumentText => $"{Name} {InvoiceNumber}".ToLowerInvariant();
    public List<string> ExtractedNumbers => ExtractNumbers();
    public List<string> ExtractedWords => ExtractWords();
    
    
    private List<string> ExtractNumbers()
    {
        var numbers = new List<string>();
        var text = $"{Name} {InvoiceNumber}";
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
        var text = Name.ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.Split(text, @"[^a-zA-Z]+")
            .Where(word => word.Length > 2)
            .ToList();
    }
    
}