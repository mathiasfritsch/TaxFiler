# Model Usage Guide

This guide explains how to load and use the trained ML model to check if documents match a given transaction.

## Overview

The DocumentTransactionMatcher uses ML.NET to predict whether a financial document matches a bank transaction. The model analyzes 6 key features to determine match probability.

## Quick Start

```csharp
using DocumentTransactionMatcher.Models;
using DocumentTransactionMatcher.Services;

// Load the trained model
var modelTrainer = new ModelTrainer();
var model = modelTrainer.LoadModel("Models/document_transaction_matcher.zip");

// Create feature extractor
var featureExtractor = new FeatureExtractor();

// Check if a document matches a transaction
var document = new DocumentModel { /* your document data */ };
var transaction = new TransactionModel { /* your transaction data */ };

var features = featureExtractor.ExtractFeatures(document, transaction);
var prediction = modelTrainer.PredictMatch(model, features);

Console.WriteLine($"Match: {prediction.IsMatch}, Confidence: {prediction.Probability:F3}");
```

## Model Loading

### Prerequisites

```csharp
using Microsoft.ML;
using DocumentTransactionMatcher.Models;
using DocumentTransactionMatcher.Services;
```

### Loading the Model

```csharp
var modelTrainer = new ModelTrainer();
var model = modelTrainer.LoadModel("Models/document_transaction_matcher.zip");
```

**Note**: Ensure the model file exists. If not, run the training process first:
```bash
dotnet run
```

## Data Models

### DocumentModel

```csharp
var document = new DocumentModel
{
    Id = 1,
    Name = "RE_CompanyName_12345.pdf",           // Required for vendor extraction
    VendorName = "Company Name",                 // Optional, preferred for vendor matching
    InvoiceNumber = "12345",                     // Optional, helps with matching
    InvoiceDate = DateTime.Parse("2025-06-15"),  // Optional, used for date proximity
    Total = 150.00m,                            // Required for amount matching
    TaxRate = 19.0m,                            // Optional
    TaxAmount = 28.50m,                         // Optional
    Skonto = 3.0m                               // Optional, early payment discount
};
```

### TransactionModel

```csharp
var transaction = new TransactionModel
{
    Id = 1,
    GrossAmount = 145.50m,                      // Required for amount matching
    TransactionDateTime = DateTime.Parse("2025-06-16"), // Required for date proximity
    SenderReceiver = "Company Name Ltd",         // Required for vendor matching
    TransactionNote = "Invoice 12345 Payment",  // Optional, helps with invoice number matching
    TaxRate = 19.0m                             // Optional
};
```

## Feature Extraction

The model uses 6 features to determine matches:

### 1. AmountSimilarity (0-1)
- **1.0**: Exact amount match
- **0.9**: Matches with documented Skonto discount
- **0.85**: Matches common 3% early payment discount
- **0-1**: Percentage similarity for other amounts

### 2. DateDiffDays (0-1)
- **1.0**: Same day
- **0.9**: Next day
- **0.8**: Within 3 days
- **0.6**: Within 7 days
- **0.4**: Within 14 days
- **0.2**: Within 30 days
- **0.0**: Over 30 days

### 3. VendorSimilarity (0-1)
Uses Levenshtein distance between:
- Document: `VendorName` (preferred) or extracted from `Name`
- Transaction: `SenderReceiver`

### 4. InvoiceNumberMatch (0-1)
- **0.9**: Invoice number found in transaction note
- **0.0**: No match

### 5. SkontoMatch (0-1)
- **1.0**: Amount matches expected Skonto-discounted amount
- **0.0**: No Skonto evidence

### 6. PatternMatch (0-1)
Compares extracted numbers and words from both document and transaction text.

## Making Predictions

### Single Document-Transaction Check

```csharp
public bool CheckDocumentMatch(DocumentModel document, TransactionModel transaction, float threshold = 0.7f)
{
    var featureExtractor = new FeatureExtractor();
    var modelTrainer = new ModelTrainer();
    var model = modelTrainer.LoadModel("Models/document_transaction_matcher.zip");
    
    var features = featureExtractor.ExtractFeatures(document, transaction);
    var prediction = modelTrainer.PredictMatch(model, features);
    
    return prediction.IsMatch && prediction.Probability >= threshold;
}
```

### Find Best Match from Document List

```csharp
public DocumentModel FindBestMatchingDocument(
    List<DocumentModel> documents, 
    TransactionModel transaction, 
    float threshold = 0.7f)
{
    var featureExtractor = new FeatureExtractor();
    var modelTrainer = new ModelTrainer();
    var model = modelTrainer.LoadModel("Models/document_transaction_matcher.zip");
    
    DocumentModel bestMatch = null;
    float highestConfidence = 0f;
    
    foreach (var document in documents)
    {
        var features = featureExtractor.ExtractFeatures(document, transaction);
        var prediction = modelTrainer.PredictMatch(model, features);
        
        if (prediction.IsMatch && 
            prediction.Probability >= threshold && 
            prediction.Probability > highestConfidence)
        {
            bestMatch = document;
            highestConfidence = prediction.Probability;
        }
    }
    
    return bestMatch;
}
```

### Get All Matching Documents with Confidence Scores

```csharp
public List<(DocumentModel Document, float Confidence)> GetAllMatches(
    List<DocumentModel> documents, 
    TransactionModel transaction, 
    float threshold = 0.5f)
{
    var matches = new List<(DocumentModel, float)>();
    var featureExtractor = new FeatureExtractor();
    var modelTrainer = new ModelTrainer();
    var model = modelTrainer.LoadModel("Models/document_transaction_matcher.zip");
    
    foreach (var document in documents)
    {
        var features = featureExtractor.ExtractFeatures(document, transaction);
        var prediction = modelTrainer.PredictMatch(model, features);
        
        if (prediction.IsMatch && prediction.Probability >= threshold)
        {
            matches.Add((document, prediction.Probability));
        }
    }
    
    // Return sorted by confidence (highest first)
    return matches.OrderByDescending(m => m.Item2).ToList();
}
```

## Understanding Predictions

### MatchingPrediction Properties

```csharp
public class MatchingPrediction
{
    public bool IsMatch { get; set; }        // True if predicted as a match
    public float Probability { get; set; }   // Confidence score (0-1)
    public float Score { get; set; }         // Raw ML model score
}
```

### Interpreting Results

- **Probability >= 0.9**: Very high confidence match
- **Probability >= 0.8**: High confidence match  
- **Probability >= 0.7**: Good confidence match (recommended threshold)
- **Probability >= 0.5**: Possible match (review recommended)
- **Probability < 0.5**: Unlikely match

## Best Practices

### 1. Confidence Thresholds
- **Production use**: 0.8+ for automatic matching
- **Review workflow**: 0.5-0.8 for human review
- **Strict matching**: 0.9+ for high-precision scenarios

### 2. Data Quality
- Provide `VendorName` when available for better vendor matching
- Ensure `InvoiceDate` and `TransactionDateTime` are accurate
- Include `InvoiceNumber` in documents for stronger matching

### 3. Performance Optimization
- Load the model once and reuse it
- Consider caching FeatureExtractor instances
- Batch predictions when checking many documents

### 4. Error Handling

```csharp
try
{
    var model = modelTrainer.LoadModel("Models/document_transaction_matcher.zip");
    // ... prediction code
}
catch (FileNotFoundException)
{
    Console.WriteLine("Model file not found. Please train the model first.");
}
catch (Exception ex)
{
    Console.WriteLine($"Prediction error: {ex.Message}");
}
```

## Example: Complete Workflow

```csharp
using DocumentTransactionMatcher.Models;
using DocumentTransactionMatcher.Services;

public class DocumentMatcher
{
    private readonly ITransformer _model;
    private readonly FeatureExtractor _featureExtractor;
    private readonly ModelTrainer _modelTrainer;
    
    public DocumentMatcher()
    {
        _modelTrainer = new ModelTrainer();
        _model = _modelTrainer.LoadModel("Models/document_transaction_matcher.zip");
        _featureExtractor = new FeatureExtractor();
    }
    
    public (DocumentModel BestMatch, float Confidence) FindBestMatch(
        List<DocumentModel> documents, 
        TransactionModel transaction)
    {
        DocumentModel bestMatch = null;
        float highestConfidence = 0f;
        
        foreach (var document in documents)
        {
            var features = _featureExtractor.ExtractFeatures(document, transaction);
            var prediction = _modelTrainer.PredictMatch(_model, features);
            
            if (prediction.IsMatch && prediction.Probability > highestConfidence)
            {
                bestMatch = document;
                highestConfidence = prediction.Probability;
            }
        }
        
        return (bestMatch, highestConfidence);
    }
}

// Usage
var matcher = new DocumentMatcher();
var documents = LoadDocuments(); // Your document loading logic
var transaction = LoadTransaction(); // Your transaction loading logic

var (bestMatch, confidence) = matcher.FindBestMatch(documents, transaction);

if (bestMatch != null)
{
    Console.WriteLine($"Best match: {bestMatch.Name} (Confidence: {confidence:F3})");
}
else
{
    Console.WriteLine("No matching document found.");
}
```

## Troubleshooting

### Model File Not Found
Ensure the model has been trained and saved:
```bash
dotnet run
```

### Low Prediction Accuracy
- Check data quality (amounts, dates, vendor names)
- Verify the model was trained on similar data
- Consider retraining with more examples

### Performance Issues
- Load the model once per application lifecycle
- Use batch prediction for multiple documents
- Consider async processing for large datasets