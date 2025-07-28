using TaxFiler.Predictor.Models;

namespace TaxFiler.Predictor.Services;

public class FeatureExtractor
{
    public MatchingFeatures ExtractFeatures(DocumentModel document, TransactionModel transaction)
    {
        return new MatchingFeatures
        {
            DocumentId = document.Id,
            TransactionId = transaction.Id,
            DocumentName = document.Name,
            TransactionNote = transaction.TransactionNote ?? string.Empty,
            AmountSimilarity = CalculateAmountSimilarity(document, transaction),
            DateDiffDays = CalculateDateDifference(document, transaction),
            VendorSimilarity = CalculateVendorSimilarity(document, transaction),
            InvoiceNumberMatch = CalculateInvoiceNumberMatch(document, transaction),
            SkontoMatch = CalculateSkontoMatch(document, transaction),
            PatternMatch = CalculatePatternMatch(document, transaction),
        };
    }
    
    private float CalculateAmountSimilarity(DocumentModel document, TransactionModel transaction)
    {
        if (!document.Total.HasValue) return 0f;
        
        var docAmount = document.Total.Value;
        var transAmount = transaction.GrossAmount;
        
        // Exact match
        if (Math.Abs(docAmount - transAmount) < 0.01m) return 1.0f;
        
        // Check for Skonto discount (typically 3% for Hays)
        if (document.Skonto.HasValue && document.Skonto.Value > 0)
        {
            var discountedAmount = docAmount * (1 - document.Skonto.Value / 100);
            if (Math.Abs(discountedAmount - transAmount) < 0.01m) return 0.9f;
        }
        
        // Check for common Skonto patterns (3% discount)
        var skontoAmount = docAmount * 0.97m; // 3% discount
        if (Math.Abs(skontoAmount - transAmount) < 0.01m) return 0.85f;
        
        // Calculate percentage difference
        var percentDiff = Math.Abs(docAmount - transAmount) / Math.Max(docAmount, transAmount);
        
        // Return similarity score (1 - percentage difference, min 0)
        return Math.Max(0f, 1f - (float)percentDiff);
    }
    
    private float CalculateDateDifference(DocumentModel document, TransactionModel transaction)
    {
        if (!document.InvoiceDate.HasValue) return 0.5f;
        
        var daysDiff = Math.Abs((transaction.TransactionDateTime.Date - document.InvoiceDate.Value.Date).Days);
        
        // Same day = 1.0, next day = 0.9, etc.
        return daysDiff switch
        {
            0 => 1.0f,
            1 => 0.9f,
            <= 3 => 0.8f,
            <= 7 => 0.6f,
            <= 14 => 0.4f,
            <= 30 => 0.2f,
            _ => 0.0f
        };
    }
    
    
    
    private float CalculateInvoiceNumberMatch(DocumentModel document, TransactionModel transaction)
    {
        var docInvoiceNumber = document.InvoiceNumber?.Trim();
        
        
        if (transaction.TransactionNote?.Contains(docInvoiceNumber, StringComparison.OrdinalIgnoreCase) == true)
            return 0.9f;
        

        return 0f;
    }
    
    
    private float CalculateSkontoMatch(DocumentModel document, TransactionModel transaction)
    {
        // If document has Skonto info and amounts suggest discount was applied
        if (document.Skonto.HasValue && document.Skonto.Value > 0 && document.Total.HasValue)
        {
            var expectedDiscountedAmount = document.Total.Value * (1 - document.Skonto.Value / 100);
            if (Math.Abs(expectedDiscountedAmount - transaction.GrossAmount) < 0.01m)
                return 1.0f;
        }
        
        return 0f;
    }
    
    private float CalculateVendorSimilarity(DocumentModel document, TransactionModel transaction)
    {
        // Prioritize explicit VendorName field, fall back to extracting from document name
        var docVendorName = !string.IsNullOrWhiteSpace(document.VendorName) 
            ? document.VendorName.Trim()
            : ExtractVendorNameFromDocument(document.Name);
            
        var transVendorName = transaction.SenderReceiver?.Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(docVendorName) || string.IsNullOrEmpty(transVendorName))
            return 0f;
        
        // Use existing string similarity calculation
        return CalculateStringSimilarity(docVendorName, transVendorName);
    }
    
    private string ExtractVendorNameFromDocument(string documentName)
    {
        if (string.IsNullOrEmpty(documentName)) return string.Empty;
        
        // Remove file extension
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(documentName);
        
        // Remove common prefixes like "RE_", "Invoice_", etc.
        var cleanName = nameWithoutExtension;
        var prefixesToRemove = new[]
        {
            "RE_", 
            "Invoice_", 
            "Rechnung_", 
            "Bill_",
            "Rng_",
            "Inv_",
            "Gsv_",
            "Gutschrift_"
        };
        
        foreach (var prefix in prefixesToRemove)
        {
            if (cleanName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleanName = cleanName.Substring(prefix.Length);
                break;
            }
        }
        
        // Extract words that might be vendor names (exclude numbers and common words)
        var words = cleanName.Split(new[] { '_', '-', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var vendorWords = words.Where(w => w.Length > 2 && !IsNumeric(w) && !IsCommonWord(w)).ToList();
        
        return string.Join(" ", vendorWords).Trim();
    }
    
    private bool IsNumeric(string str)
    {
        return str.All(char.IsDigit);
    }
    
    private bool IsCommonWord(string word)
    {
        var commonWords = new[] { "pdf", "doc", "docx", "txt", "rechnung", "invoice", "bill", "receipt" };
        return commonWords.Contains(word.ToLowerInvariant());
    }
    
    private float CalculatePatternMatch(DocumentModel document, TransactionModel transaction)
    {
        var score = 0f;
        var maxScore = 0f;
        
        // Compare extracted numbers
        var docNumbers = document.ExtractedNumbers;
        var transNumbers = transaction.ExtractedNumbers;
        
        if (docNumbers.Any() && transNumbers.Any())
        {
            maxScore += 0.6f;
            var numberMatches = docNumbers.Count(docNum => 
                transNumbers.Any(transNum => 
                    docNum.Equals(transNum, StringComparison.OrdinalIgnoreCase) ||
                    (docNum.Length >= 4 && transNum.Length >= 4 && 
                     docNum.Contains(transNum.Substring(Math.Max(0, transNum.Length - 4))) ||
                     transNum.Contains(docNum.Substring(Math.Max(0, docNum.Length - 4))))));
            
            if (numberMatches > 0)
                score += 0.6f * Math.Min(1f, (float)numberMatches / Math.Max(docNumbers.Count, transNumbers.Count));
        }
        
        // Compare extracted words
        var docWords = document.ExtractedWords;
        var transWords = transaction.ExtractedWords;
        
        if (docWords.Any() && transWords.Any())
        {
            maxScore += 0.4f;
            var wordMatches = docWords.Count(docWord => 
                transWords.Any(transWord => 
                    docWord.Equals(transWord, StringComparison.OrdinalIgnoreCase) ||
                    (docWord.Length >= 4 && transWord.Length >= 4 && 
                     CalculateStringSimilarity(docWord, transWord) > 0.8f)));
            
            if (wordMatches > 0)
                score += 0.4f * Math.Min(1f, (float)wordMatches / Math.Max(docWords.Count, transWords.Count));
        }
        
        // Return normalized score
        return maxScore > 0 ? score / maxScore : 0f;
    }
    
    
    private float CalculateStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2)) return 0f;
        
        var distance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        
        return 1f - (float)distance / maxLength;
    }
    
    private int LevenshteinDistance(string str1, string str2)
    {
        var matrix = new int[str1.Length + 1, str2.Length + 1];
        
        for (int i = 0; i <= str1.Length; i++)
            matrix[i, 0] = i;
        
        for (int j = 0; j <= str2.Length; j++)
            matrix[0, j] = j;
        
        for (int i = 1; i <= str1.Length; i++)
        {
            for (int j = 1; j <= str2.Length; j++)
            {
                var cost = str1[i - 1] == str2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }
        
        return matrix[str1.Length, str2.Length];
    }
}
