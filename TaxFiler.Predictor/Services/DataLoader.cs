using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TaxFiler.Predictor.Models;

namespace TaxFiler.Predictor.Services;

public class DataLoader
{
    private readonly FeatureExtractor _featureExtractor;

    public DataLoader()
    {
        _featureExtractor = new FeatureExtractor();
    }
    
    public List<DocumentModel> LoadDocuments(string filePath)
    {
        using var reader = new StringReader(File.ReadAllText(filePath));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Configure to not use headers and handle null values
        csv.Context.Configuration.HasHeaderRecord = false;
        csv.Context.Configuration.MissingFieldFound = null;
        csv.Context.Configuration.BadDataFound = null;
        csv.Context.TypeConverterCache.AddConverter<DateTime?>(new NullableDateTimeConverter());
        csv.Context.TypeConverterCache.AddConverter<decimal?>(new NullableDecimalConverter());
        csv.Context.TypeConverterCache.AddConverter<int?>(new NullableIntConverter());
        csv.Context.RegisterClassMap<DocumentModelMap>();
        return csv.GetRecords<DocumentModel>().ToList();
    }
    
    public List<TransactionModel> LoadTransactions(string filePath)
    {
        using var reader = new StringReader(File.ReadAllText(filePath));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Configure to not use headers and handle null values
        csv.Context.Configuration.HasHeaderRecord = false;
        csv.Context.Configuration.MissingFieldFound = null;
        csv.Context.Configuration.BadDataFound = null;
        csv.Context.TypeConverterCache.AddConverter<DateTime?>(new NullableDateTimeConverter());
        csv.Context.TypeConverterCache.AddConverter<decimal?>(new NullableDecimalConverter());
        csv.Context.TypeConverterCache.AddConverter<int?>(new NullableIntConverter());
        csv.Context.TypeConverterCache.AddConverter<int>(new IntConverter());
        csv.Context.TypeConverterCache.AddConverter<bool?>(new NullableBooleanConverter());
        csv.Context.RegisterClassMap<TransactionModelMap>();
        return csv.GetRecords<TransactionModel>().ToList();
    }
    
    public List<MatchingFeatures> GenerateTrainingData(
        List<DocumentModel> documents, 
        List<TransactionModel> transactions)
    {
        var trainingData = new List<MatchingFeatures>();
        
        // Generate positive examples from known matches
        var positiveExamples = GeneratePositiveExamples(documents, transactions);
        trainingData.AddRange(positiveExamples);
        
        // Generate negative examples
        var negativeExamples = GenerateNegativeExamples(documents, transactions, positiveExamples.Count * 2);
        trainingData.AddRange(negativeExamples);
        
        // Shuffle the data
        var random = new Random(42);
        return trainingData.OrderBy(x => random.Next()).ToList();
    }
    
    private List<MatchingFeatures> GeneratePositiveExamples(
        List<DocumentModel> documents, 
        List<TransactionModel> transactions)
    {
        var positiveExamples = new List<MatchingFeatures>();
        
        // Use DocumentId in transactions to find confirmed matches
        foreach (var transaction in transactions.Where(t => t.DocumentId.HasValue))
        {
            var document = documents.FirstOrDefault(d => d.Id == transaction.DocumentId.Value);
            if (document != null)
            {
                var features = _featureExtractor.ExtractFeatures(document, transaction);
                features.IsMatch = true;
                positiveExamples.Add(features);
            }
        }
        
        Console.WriteLine($"Found {positiveExamples.Count} confirmed document-transaction matches using DocumentId");
        
        return positiveExamples;
    }
    
    
    private List<MatchingFeatures> GenerateNegativeExamples(
        List<DocumentModel> documents,
        List<TransactionModel> transactions,
        int count)
    {
        var negativeExamples = new List<MatchingFeatures>();
        var random = new Random(42);
        var attempts = 0;
        var maxAttempts = count * 10;
        
        // Get confirmed matches to avoid them in negative examples
        var confirmedMatches = transactions
            .Where(t => t.DocumentId.HasValue)
            .Select(t => new { DocumentId = t.DocumentId.Value, TransactionId = t.Id })
            .ToHashSet();
        
        while (negativeExamples.Count < count && attempts < maxAttempts)
        {
            attempts++;
            
            var document = documents[random.Next(documents.Count)];
            var transaction = transactions[random.Next(transactions.Count)];
            
            // Skip if this is a confirmed match
            if (confirmedMatches.Contains(new { DocumentId = document.Id, TransactionId = transaction.Id }))
                continue;
            
            var features = _featureExtractor.ExtractFeatures(document, transaction);
            
            // Consider it a negative example if multiple features suggest no match
            var totalScore = features.AmountSimilarity + features.DateDiffDays +
                           features.VendorSimilarity + features.InvoiceNumberMatch;

            if (totalScore < 2.0f) // Threshold for "clearly not a match"
            {
                features.IsMatch = false;
                negativeExamples.Add(features);
            }
        }
        
        Console.WriteLine($"Generated {negativeExamples.Count} negative examples (avoiding {confirmedMatches.Count} confirmed matches)");
        return negativeExamples;
    }
    
    public void SaveTrainingData(List<MatchingFeatures> trainingData, string filePath)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        csv.Context.RegisterClassMap<MatchingFeaturesMap>();
        csv.WriteRecords(trainingData);
        
        File.WriteAllText(filePath, writer.ToString());
    }
}

// CSV mapping classes
public class DocumentModelMap : ClassMap<DocumentModel>
{
    public DocumentModelMap()
    {
        Map(m => m.Id).Index(0);
        Map(m => m.Name).Index(1);
        Map(m => m.ExternalRef).Index(2);
        Map(m => m.Orphaned).Index(3);
        Map(m => m.Parsed).Index(4);
        Map(m => m.InvoiceNumber).Index(5);
        Map(m => m.InvoiceDate).Index(6);
        Map(m => m.SubTotal).Index(7);
        Map(m => m.Total).Index(8);
        Map(m => m.TaxRate).Index(9);
        Map(m => m.TaxAmount).Index(10);
        Map(m => m.Skonto).Index(11);
        Map(m => m.VendorName).Index(12);
        Map(m => m.InvoiceDateFromFolder).Index(13);
    }
}

public class TransactionModelMap : ClassMap<TransactionModel>
{
    public TransactionModelMap()
    {
        Map(m => m.Id).Index(0);
        Map(m => m.AccountId).Index(1);
        Map(m => m.NetAmount).Index(2);
        Map(m => m.GrossAmount).Index(3);
        Map(m => m.TaxAmount).Index(4);
        Map(m => m.TaxRate).Index(5);
        Map(m => m.Counterparty).Index(6);
        Map(m => m.TransactionReference).Index(7);
        Map(m => m.TransactionDateTime).Index(8);
        Map(m => m.TransactionNote).Index(9);
        Map(m => m.IsOutgoing).Index(10);
        Map(m => m.IsIncomeTaxRelevant).Index(11);
        Map(m => m.IsSalesTaxRelevant).Index(12);
        Map(m => m.TaxMonth).Index(13);
        Map(m => m.TaxYear).Index(14);
        Map(m => m.DocumentId).Index(15);
        Map(m => m.SenderReceiver).Index(16);
    }
}

public class MatchingFeaturesMap : ClassMap<MatchingFeatures>
{
    public MatchingFeaturesMap()
    {
        Map(m => m.AmountSimilarity).Name("AmountSimilarity");
        Map(m => m.DateDiffDays).Name("DateDiffDays");
        Map(m => m.VendorSimilarity).Name("VendorSimilarity");
        Map(m => m.InvoiceNumberMatch).Name("InvoiceNumberMatch");
        Map(m => m.SkontoMatch).Name("SkontoMatch");
        Map(m => m.PatternMatch).Name("PatternMatch");
        Map(m => m.IsMatch).Name("IsMatch");
        Map(m => m.DocumentId).Name("DocumentId");
        Map(m => m.TransactionId).Name("TransactionId");
        Map(m => m.DocumentName).Name("DocumentName");
        Map(m => m.TransactionNote).Name("TransactionNote");
    }
}

// Custom type converters to handle "null" strings
public class NullableDateTimeConverter : CsvHelper.TypeConversion.DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, CsvHelper.IReaderRow row, CsvHelper.Configuration.MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (DateTime.TryParse(text, out var result))
            return result;

        return null;
    }
}

public class NullableDecimalConverter : CsvHelper.TypeConversion.DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, CsvHelper.IReaderRow row, CsvHelper.Configuration.MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (decimal.TryParse(text, out var result))
            return result;

        return null;
    }
}

public class NullableIntConverter : CsvHelper.TypeConversion.DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, CsvHelper.IReaderRow row, CsvHelper.Configuration.MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (int.TryParse(text, out var result))
            return result;

        return null;
    }
}

public class IntConverter : CsvHelper.TypeConversion.DefaultTypeConverter
{
    public override object ConvertFromString(string? text, CsvHelper.IReaderRow row, CsvHelper.Configuration.MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase))
            return 0; // Return default value for non-nullable int

        if (int.TryParse(text, out var result))
            return result;

        return 0; // Return default value if parsing fails
    }
}

public class NullableBooleanConverter : CsvHelper.TypeConversion.DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, CsvHelper.IReaderRow row, CsvHelper.Configuration.MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (bool.TryParse(text, out var result))
            return result;

        return null;
    }
}