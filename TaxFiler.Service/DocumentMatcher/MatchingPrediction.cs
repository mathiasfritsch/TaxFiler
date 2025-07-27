namespace TaxFiler.Service.DocumentMatcher;

public class MatchingPrediction
{
    public bool IsMatch { get; set; }

    public float Probability { get; set; }
    
    public float Score { get; set; }
}