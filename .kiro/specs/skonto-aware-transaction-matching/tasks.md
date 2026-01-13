# Implementation Plan: Skonto-Aware Transaction Matching

## Overview

This implementation plan converts the Skonto-Aware Transaction Matching design into a series of incremental coding tasks. Each task builds on previous work and focuses on enhancing the existing amount matching logic to properly handle early payment discounts (Skonto) while maintaining backward compatibility.

## Tasks

- [-] 1. Create Skonto calculation utilities
  - [x] 1.1 Create `SkontoCalculator` static class
    - Implement `CalculateDiscountedAmount` method for percentage-based discount calculation
    - Implement `HasValidSkonto` method for Skonto validation
    - Handle edge cases (null, zero, negative percentages)
    - _Requirements: 1.1, 1.2, 1.5_

  - [ ]* 1.2 Write property tests for Skonto calculation
    - **Property 1: Skonto Amount Calculation**
    - **Validates: Requirements 1.1, 1.2**

  - [x] 1.3 Write unit tests for edge cases

    - Test null, zero, and negative Skonto percentages
    - Test boundary conditions and rounding scenarios
    - _Requirements: 1.5_

- [-] 2. Enhance existing AmountMatcher class
  - [x] 2.1 Modify `CalculateAmountScore` method to detect and apply Skonto
    - Add Skonto detection logic using `SkontoCalculator.HasValidSkonto`
    - Apply Skonto discount calculation when present
    - Maintain existing logic for documents without Skonto
    - _Requirements: 1.1, 1.3_

  - [ ]* 2.2 Write property tests for enhanced amount matching
    - **Property 2: Skonto-Adjusted Matching**
    - **Property 3: Backward Compatibility for Non-Skonto Documents**
    - **Property 4: Skonto Scoring Behavior**
    - **Validates: Requirements 1.1, 1.3, 1.4**

  - [ ]* 2.3 Write unit tests for specific scenarios
    - Test known Skonto calculation examples
    - Test integration with existing tolerance logic
    - Test document amount selection priority
    - _Requirements: 1.1, 1.4_

- [ ] 3. Add comprehensive edge case handling
  - [x] 3.1 Enhance error handling in AmountMatcher
    - Handle invalid Skonto percentages gracefully
    - Add validation for calculation results
    - Ensure no negative amounts from Skonto calculations
    - _Requirements: 1.5_

  - [ ]* 3.2 Write property tests for edge case handling
    - **Property 5: Edge Case Handling**
    - **Validates: Requirements 1.5**

- [x] 4. Checkpoint - Ensure all enhanced matching tests pass
  - Ensure all enhanced amount matching tests pass, ask the user if questions arise.

- [-] 5. Integration testing and validation
  - [x] 5.1 Test integration with existing DocumentMatchingService
    - Verify Skonto-aware matching works with full document matching pipeline
    - Test with realistic German tax document data containing Skonto terms
    - Validate performance impact is minimal
    - _Requirements: 1.1, 1.3_

  - [ ]* 5.2 Write integration tests
    - Test end-to-end matching with Skonto documents
    - Test backward compatibility with existing document sets
    - Test API response formats remain unchanged
    - _Requirements: 1.1, 1.3_

- [-] 6. Final validation and testing
  - [x] 6.1 Validate backward compatibility
    - Ensure existing tests still pass
    - Verify no breaking changes to public interfaces
    - Test with documents that have no Skonto terms
    - _Requirements: 1.3_

  - [ ]* 6.2 Write comprehensive property tests
    - Test with large document sets containing mixed Skonto scenarios
    - Test with realistic German tax document data
    - Validate scoring consistency across different Skonto percentages
    - _Requirements: 1.1, 1.2, 1.4_

- [x] 7. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- The implementation enhances existing AmountMatcher without breaking changes
- Skonto calculations use decimal precision for financial accuracy
- All existing API contracts and response formats are preserved