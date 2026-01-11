# Implementation Plan: Enhanced Transaction Document Matching

## Overview

This implementation plan converts the Enhanced Transaction Document Matching design into a series of incremental coding tasks. Each task builds on previous work and focuses on creating a robust, testable matching system that integrates seamlessly with the existing TaxFiler architecture.

## Tasks

- [ ] 1. Set up core interfaces and configuration models
  - Create `IDocumentMatchingService` interface in TaxFiler.Service
  - Create configuration classes (`MatchingConfiguration`, `AmountMatchingConfig`, etc.)
  - Create result classes (`DocumentMatch`, `MatchScoreBreakdown`)
  - _Requirements: 1.1, 6.1, 8.1_

- [ ] 2. Implement string similarity utilities
  - [ ] 2.1 Create `StringSimilarity` static class with Levenshtein distance algorithm
    - Implement `LevenshteinSimilarity` method for fuzzy string matching
    - Implement `ContainsIgnoreCase` and `NormalizeForMatching` helper methods
    - _Requirements: 4.4, 4.5, 5.4_

  - [ ]* 2.2 Write property test for string similarity utilities
    - **Property 11: Reference Number Matching**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5**

- [ ] 3. Implement individual matching components
  - [ ] 3.1 Create `AmountMatcher` class
    - Implement amount comparison logic with tolerance ranges
    - Handle SubTotal, Total, TaxAmount, and Skonto fields
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ]* 3.2 Write property tests for amount matching
    - **Property 5: Amount Scoring Tolerance Ranges**
    - **Property 6: Document Amount Field Handling**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6**

  - [ ] 3.3 Create `DateMatcher` class
    - Implement date proximity scoring with configurable tolerances
    - Handle InvoiceDate and InvoiceDateFromFolder priority
    - Handle null/missing date fields gracefully
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [ ]* 3.4 Write property tests for date matching
    - **Property 7: Date Proximity Scoring**
    - **Property 8: Date Field Priority and Null Handling**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**

  - [ ] 3.5 Create `VendorMatcher` class
    - Implement vendor name matching hierarchy (exact, substring, fuzzy)
    - Use both Counterparty and SenderReceiver fields from transactions
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [ ]* 3.6 Write property tests for vendor matching
    - **Property 9: Vendor Name Matching Hierarchy**
    - **Property 10: Multiple Vendor Field Usage**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.6**

  - [ ] 3.7 Create `ReferenceMatcher` class
    - Implement reference number matching with case-insensitive comparison
    - Handle null/empty reference fields
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 4. Checkpoint - Ensure individual matcher tests pass
  - Ensure all individual matcher tests pass, ask the user if questions arise.

- [ ] 5. Implement core document matching service
  - [ ] 5.1 Create `DocumentMatchingService` class
    - Implement `DocumentMatchesAsync` method for single transactions
    - Implement composite scoring algorithm with configurable weights
    - Apply bonus multiplier for high individual scores
    - Filter results by minimum score threshold
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 6.1, 6.2, 6.3, 6.5_

  - [ ]* 5.2 Write property tests for core matching service
    - **Property 1: Document Matching Returns Properly Ranked Results**
    - **Property 2: Empty Results for Poor Matches**
    - **Property 3: Multi-Criteria Scoring Behavior**
    - **Property 12: Bonus Score Application**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 6.1, 6.2, 6.3, 6.5**

  - [ ] 5.3 Implement batch matching functionality
    - Add `BatchDocumentMatchesAsync` method for multiple transactions
    - Ensure batch operations produce consistent results with individual matching
    - _Requirements: 7.4_

  - [ ]* 5.4 Write property test for batch operations
    - **Property 13: Batch Operation Consistency**
    - **Validates: Requirements 7.4**

- [ ] 6. Add configuration support and validation
  - [ ] 6.1 Implement configuration validation
    - Validate weight values sum to reasonable ranges
    - Validate threshold values are within 0.0-1.0 range
    - Validate tolerance values are positive
    - _Requirements: 8.4_

  - [ ]* 6.2 Write property tests for configuration
    - **Property 14: Configuration Weight Effects**
    - **Property 15: Configuration Validation**
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.4**

  - [ ] 6.3 Add transaction direction independence
    - Ensure matching works consistently for both incoming and outgoing transactions
    - _Requirements: 1.5_

  - [ ]* 6.4 Write property test for transaction direction independence
    - **Property 4: Transaction Direction Independence**
    - **Validates: Requirements 1.5**

- [ ] 7. Checkpoint - Ensure core service tests pass
  - Ensure all core service tests pass, ask the user if questions arise.

- [ ] 8. Integration and dependency injection setup
  - [ ] 8.1 Register services in dependency injection container
    - Add service registrations in `Program.cs`
    - Configure default matching configuration
    - Set up Entity Framework integration
    - _Requirements: 1.1_

  - [ ] 8.2 Create API controller for document matching
    - Add `DocumentMatchingController` with endpoints for single and batch matching
    - Implement proper error handling and response formatting
    - Add API documentation with Swagger annotations
    - _Requirements: 1.1, 7.4_

  - [ ]* 8.3 Write integration tests for API controller
    - Test API endpoints with realistic data
    - Test error handling scenarios
    - Test response formatting and status codes
    - _Requirements: 1.1, 7.4_

- [ ] 9. Performance optimization and caching
  - [ ] 9.1 Implement efficient database queries
    - Optimize Entity Framework queries to minimize data retrieval
    - Add appropriate database indexes for matching fields
    - _Requirements: 7.2_

  - [ ] 9.2 Add basic caching for frequently accessed data
    - Implement in-memory caching for configuration data
    - Cache document data for repeated matching operations
    - _Requirements: 7.3_

- [ ] 10. Final integration and testing
  - [ ] 10.1 Wire all components together
    - Ensure all services are properly integrated
    - Test end-to-end functionality with realistic data
    - Verify performance meets requirements
    - _Requirements: 7.1_

  - [ ]* 10.2 Write comprehensive integration tests
    - Test with large document sets (up to 1000 documents)
    - Test with realistic German tax document data
    - Test configuration changes and their effects
    - _Requirements: 7.1, 8.5_

- [ ] 11. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- The implementation uses Entity Framework Core for data access
- String similarity algorithms use Levenshtein distance for fuzzy matching
- Configuration is validated to ensure system stability