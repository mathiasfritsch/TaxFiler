# ML-Powered Best Match Endpoint Implementation Plan

## Overview
Add an API endpoint that returns the best matching document for a given transaction using the trained ML.NET model. The endpoint will only consider documents that are not already matched to other transactions.

## Implementation Steps

### 1. Research Existing ML Implementation
- [ ] Examine TaxFiler.Predictor project structure and ML models
- [ ] Review existing ML.NET implementation patterns
- [ ] Understand current model training and prediction workflows

### 2. Locate ML Model File
- [ ] Find the document_transaction_matcher.zip model file in the TaxFiler.Predictor project
- [ ] Verify model file integrity and compatibility

### 3. Copy Model to Web Project
- [ ] Copy document_transaction_matcher.zip from TaxFiler.Predictor to TaxFiler.Server project
- [ ] Ensure model file is accessible at runtime
- [ ] Update project configuration to include model file in build output

### 4. Add ML.NET Dependencies
- [ ] Check if ML.NET NuGet packages are already present in TaxFiler.Server
- [ ] Add required ML.NET packages if missing
- [ ] Ensure package versions are compatible with existing codebase

### 5. Create API Endpoint
- [ ] Add new controller method to accept transaction input
- [ ] Define appropriate HTTP method and route
- [ ] Implement request/response models for best match functionality
- [ ] Add proper error handling and validation

### 6. Filter Unmatched Documents
- [ ] Query database for documents not already linked to transactions
- [ ] Optimize database queries for performance

### 7. Implement ML Prediction Service
- [ ] Create service method to load ML model from document_transaction_matcher.zip
- [ ] Implement prediction logic for document-transaction matching
- [ ] Score all potential document matches
- [ ] Handle model loading exceptions and fallback scenarios

### 8. Return Best Match
- [ ] Sort prediction results by confidence score
- [ ] Return the highest scoring document match
- [ ] Include confidence score and relevant metadata in response
- [ ] Handle cases where no suitable match is found

## Technical Considerations

- **Performance**: Model loading should be optimized (consider caching loaded model)
- **Error Handling**: Graceful handling of ML prediction failures
- **Database Queries**: Efficient filtering of unmatched documents
- **API Design**: RESTful endpoint design following existing patterns
- **Response Format**: Consistent with existing API response structures

## Deliverables

1. Updated TaxFiler.Server project with document_transaction_matcher.zip model file
2. New API endpoint for best match prediction
3. Service layer implementation for ML prediction logic
4. Database query optimization for unmatched document filtering
5. Proper error handling and validation

## Notes

- No unit tests will be added as per requirements
- Only documents not currently matched to transactions will be considered
- Implementation should follow existing codebase patterns and conventions