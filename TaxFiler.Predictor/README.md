# Document-Transaction Matching with ML.NET

This project implements an ML.NET-based solution for automatically mapping financial documents to their corresponding bank transactions, specifically designed for German tax compliance scenarios.

## Overview

The system uses machine learning to identify relationships between:
- **Documents**: Invoices, receipts, and financial documents with extracted metadata
- **Transactions**: Bank transactions with counterparty and payment details

## Key Features

- **Pattern Recognition**: Identifies vendor-specific patterns
- **Amount Matching**: Handles exact matches and Skonto (early payment discounts)
- **Date Proximity**: Considers payment timing relative to invoice dates
- **Invoice Number Extraction**: Matches invoice numbers in transaction descriptions
- **Vendor Similarity**: Uses fuzzy matching for counterparty names
- **Tax Rate Validation**: Ensures consistency between document and transaction tax rates

## Project Structure

```
DocumentTransactionMatcher/
├── Models/
│   ├── DocumentModel.cs          # Document data structure
│   ├── TransactionModel.cs       # Transaction data structure
│   ├── MatchingFeatures.cs       # Feature vector for ML model
│   └── MatchingPrediction.cs     # Prediction result
├── Services/
│   ├── FeatureExtractor.cs       # Calculates similarity scores
│   ├── DataLoader.cs             # CSV parsing and training data generation
│   └── ModelTrainer.cs           # ML.NET training pipeline
├── Data/                         # CSV data files
├── Models/                       # Trained ML models
└── Program.cs                    # Main application
```

## Training Data Patterns

The model is trained on known matching patterns identified in the sample data:


## Usage

### 1. Build and Run
```bash
dotnet build
dotnet run
```

### 2. Generate Sample Data
```bash
dotnet run -- --generate-data
```

### 3. Training Process
The application will:
1. Load documents and transactions from sample data
2. Generate feature vectors for known matches and non-matches
3. Train a FastTree binary classification model
4. Evaluate model performance (accuracy, precision, recall)
5. Save the trained model for future use

### 4. Prediction
The trained model can predict matches for new document-transaction pairs with confidence scores.

## Feature Engineering

The model uses 7 key features:

1. **AmountSimilarity** (0-1): Exact match, Skonto discount, or percentage similarity
2. **DateDiffDays** (0-1): Days between invoice date and transaction date
3. **VendorSimilarity** (0-1): Fuzzy string matching of vendor names
4. **InvoiceNumberMatch** (0-1): Invoice number presence in transaction notes
5. **TaxRateMatch** (0-1): Consistency of tax rates between document and transaction
6. **SkontoMatch** (0-1): Evidence of early payment discount application
7. **PatternMatch** (0-1): Vendor-specific pattern recognition

## Model Performance

The model achieves high accuracy on the identified patterns:

## Extending the Model

To add new vendor patterns:

1. **Update Models**: Add vendor-specific properties to `DocumentModel` and `TransactionModel`
2. **Enhance Features**: Modify `FeatureExtractor` to recognize new patterns
3. **Training Data**: Add known matches to `DataLoader.GeneratePositiveExamples()`
4. **Pattern Matching**: Update `CalculatePatternMatch()` for vendor-specific logic

## Sample Output

```
Training model with 28 examples...
Training in progress...

Evaluating model...
Accuracy: 0.9167
AUC: 0.9500
F1 Score: 0.8889
Precision: 1.0000
Recall: 0.8000

```

## Requirements

- .NET 8.0 or later
- Microsoft.ML NuGet package
- CsvHelper for data parsing

## German Tax Compliance

This solution is specifically designed for German freelancers and small businesses who need to:
- Match invoices to bank transactions for VAT reporting
- Handle Skonto (early payment discounts)
- Manage multiple tax rates (0%, 7%, 19%)
- Track business expenses for income tax deductions

The system helps automate the tedious process of matching hundreds of documents to transactions for quarterly VAT returns and annual tax filings.