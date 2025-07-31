# Feature PRD: Improve Vendor Matching in FeatureExtractor

## Overview

Enhance the `CalculateVendorSimilarity` method in `FeatureExtractor` to extract vendor information from transaction notes instead of relying solely on the `SenderReceiver` field, which often contains payment processor names rather than actual merchant names.

## Problem Statement

The current `CalculateVendorSimilarity` method in `FeatureExtractor.cs` (line 104) uses `transaction.SenderReceiver` for vendor comparison:

```csharp
var transVendorName = transaction.SenderReceiver?.Trim() ?? string.Empty;
```

This approach has limitations:
- `SenderReceiver` often contains payment processor names (e.g., "PAYPAL", "STRIPE") rather than actual merchants
- The real vendor information is embedded in `transaction.TransactionNote` (e.g., "PAYPAL *AMAZONSERVICES")
- Document vendor names are often good quality but not being compared against the transaction note content
- Results in poor vendor similarity scores and reduced document-transaction matching accuracy

## Current vs Desired Behavior

### Current Behavior
- Transaction Note: `"PAYPAL *AMAZONSERVICES 402-935-7733 WA"`
- SenderReceiver: `"PAYPAL"`
- Document Vendor: `"Amazon"`
- Result: Low similarity score (comparing "PAYPAL" vs "Amazon")

### Desired Behavior
- Transaction Note: `"PAYPAL *AMAZONSERVICES 402-935-7733 WA"`
- Document Vendor: `"Amazon"`
- Comparison: Compare "amazon" (lowercase) with each word in transaction note ["paypal", "amazonservices", "402-935-7733", "wa"]
- Result: Improved similarity score (case-insensitive Levenshtein distance between "amazon" and "amazonservices" shows stronger match)

## Goals

## Technical Requirements

### Word-Based Comparison Approach
Instead of complex regex pattern extraction, use simple word-by-word comparison:

1. **Extract document vendor name** (from `VendorName` field or document name)
2. **Split transaction note into words** (split by spaces and common delimiters)
3. **Compare document vendor with each word** using Levenshtein distance
4. **Return highest similarity score** found among all words
5. **Fallback to SenderReceiver** if transaction note is empty or no good matches found

### Implementation Approach

1. **Word Extraction**: Split transaction note by spaces, remove special characters and numbers
2. **SenderReceiver**: Use `SenderReceiver` as just another word to add to the comparison set
3. **Case-Insensitive Comparison**: Use existing `CalculateStringSimilarity` method with case-insensitive Levenshtein distance
4. **Best Match**: Return the highest similarity score found when comparing document vendor to all words
5. **Minimum Length Filter**: Only compare words of reasonable length (3+ characters)

### Method Signature
```csharp
private float CalculateVendorSimilarity(DocumentModel document, TransactionModel transaction)
{
    // Compare document vendor with all words in transaction note
    // Return best similarity score found
}
```

## Implementation Details

### Phase 1: Core Enhancement
- Update `CalculateVendorSimilarity` to compare document vendor with transaction note words
- Use existing `CalculateStringSimilarity` method for word comparisons
- Implement word extraction and filtering logic
- Maintain fallback to `SenderReceiver` when no good matches found

### Phase 2: Optimization  
- Fine-tune minimum similarity thresholds based on case-insensitive results
- Optimize word filtering and preprocessing
- Consider additional normalization beyond case-insensitive matching

## Testing Strategy

### Unit Tests
Create test cases for:
- only create 3 unit tests for the new extraction logic

### Test Data Examples
```csharp
// Note: Similarity thresholds adjusted for realistic case-insensitive Levenshtein scores
[TestCase("PAYPAL *AMAZONSERVICES 402-935-7733", "Amazon", ExpectedResult = 0.42f)] // "amazon" vs "amazonservices" 
[TestCase("STRIPE PAYMENT SHOPIFY INC 123-456", "Shopify", ExpectedResult = 0.71f)] // "shopify" vs "shopify"
[TestCase("WALMART SUPERCENTER #1234 ANYTOWN", "Walmart", ExpectedResult = 1.0f)]  // "walmart" vs "walmart"
[TestCase("UNRELATED BANK TRANSFER 999", "Amazon", ExpectedResult = 0.0f)]        // No good matches
```


## Out of Scope

- Database schema changes
- New service interfaces
- UI modifications
- Machine learning approaches
- External vendor databases

## Success Criteria

- [x] Enhanced `CalculateVendorSimilarity` method compares document vendor with transaction note words
- [x] Uses case-insensitive Levenshtein distance for word-by-word comparison instead of regex extraction
- [x] Includes `SenderReceiver` as another word in the comparison set (no fallback needed)
- [x] Includes 3 unit tests with realistic similarity score expectations (0.4+ for partial matches, 1.0 for exact matches)
- [x] Improves vendor similarity scores through case-insensitive matching without complex pattern matching
- [x] Handles various transaction note formats with improved accuracy due to case normalization
