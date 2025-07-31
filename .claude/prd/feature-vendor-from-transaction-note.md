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
- Comparison: Compare "Amazon" with each word in transaction note ["PAYPAL", "AMAZONSERVICES", "402-935-7733", "WA"]
- Result: High similarity score (Levenshtein distance between "Amazon" and "AMAZONSERVICES" shows strong match)

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
2.  **SenderReceiver**: Use `SenderReceiver` as just another word to add to the comparison set
3.  **String Similarity**: Use existing `CalculateStringSimilarity` method with Levenshtein distance
4.  **Best Match**: Return the highest similarity score found when comparing document vendor to all words
5.  **Minimum Length Filter**: Only compare words of reasonable length (3+ characters)

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
- Fine-tune minimum similarity thresholds
- Optimize word filtering and preprocessing
- Add basic word normalization (case-insensitive, remove common prefixes/suffixes)

## Testing Strategy

### Unit Tests
Create test cases for:
- only create 3 unit tests for the new extraction logic

### Test Data Examples
```csharp
[TestCase("PAYPAL *AMAZONSERVICES 402-935-7733", "Amazon", ExpectedResult = true)]
[TestCase("STRIPE PAYMENT SHOPIFY INC 123-456", "Shopify", ExpectedResult = true)]
[TestCase("WALMART SUPERCENTER #1234 ANYTOWN", "Walmart", ExpectedResult = true)]
[TestCase("UNRELATED BANK TRANSFER 999", "Amazon", ExpectedResult = false)]
```


## Out of Scope

- Database schema changes
- New service interfaces
- UI modifications
- Machine learning approaches
- External vendor databases

## Success Criteria

- [x] Enhanced `CalculateVendorSimilarity` method compares document vendor with transaction note words
- [x] Uses Levenshtein distance for word-by-word comparison instead of regex extraction
- [x] Maintains backward compatibility with existing `SenderReceiver` fallback logic
- [x] Includes 3 unit tests covering different transaction note formats
- [x] Improves vendor similarity scores for common transaction types without complex pattern matching
