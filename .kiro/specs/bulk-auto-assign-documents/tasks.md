# Implementation Plan: Bulk Auto-Assign Documents

## Overview

This implementation plan breaks down the Bulk Auto-Assign Documents feature into discrete coding tasks. The feature adds a one-click button to the transactions month view that automatically assigns the best matching documents to unmatched transactions using the existing document matching algorithm.

## Tasks

- [x] 1. Create AutoAssignResult data model
  - Create `AutoAssignResult.cs` in `TaxFiler.Model/Dto/` with properties for TotalProcessed, AssignedCount, SkippedCount, and Errors list
  - Create corresponding TypeScript interface in `taxfiler.client/src/app/model/`
  - _Requirements: 4.1, 4.2, 4.3_

- [x] 2. Implement backend auto-assignment service
  - [x] 2.1 Add AutoAssignDocumentsAsync method to ITransactionService interface
    - Add method signature with yearMonth parameter and CancellationToken
    - _Requirements: 1.2, 2.2, 3.2, 3.3_

  - [x] 2.2 Implement AutoAssignDocumentsAsync in TransactionService
    - Query unmatched transactions for the specified month (DocumentId == null)
    - Call BatchDocumentMatchesAsync to get matches for all transactions
    - Iterate through transactions and assign best match if score >= 0.5
    - Handle errors per transaction and continue processing
    - Save all changes in a single database transaction
    - Return AutoAssignResult with counts and errors
    - _Requirements: 1.2, 2.2, 2.4, 3.2, 3.3, 3.4_

  - [ ]* 2.3 Write property test for unassigned transaction filtering
    - **Property 1: Only unassigned transactions processed**
    - **Validates: Requirements 1.2**

  - [ ]* 2.4 Write property test for threshold enforcement
    - **Property 2: Threshold enforcement**
    - **Validates: Requirements 2.2**

  - [ ]* 2.5 Write property test for unconnected documents
    - **Property 3: Unconnected documents only**
    - **Validates: Requirements 2.4**

  - [ ]* 2.6 Write property test for processing order
    - **Property 4: Processing order preservation**
    - **Validates: Requirements 3.2**

  - [ ]* 2.7 Write property test for error resilience
    - **Property 5: Error resilience**
    - **Validates: Requirements 3.3**

  - [ ]* 2.8 Write property test for data integrity on interruption
    - **Property 6: Data integrity on interruption**
    - **Validates: Requirements 5.4**

  - [x] 2.9 Write unit tests for AutoAssignDocumentsAsync

    - Test empty transaction list
    - Test all transactions already assigned
    - Test mix of assigned and unassigned transactions
    - Test cancellation token triggered mid-operation
    - Test error handling for individual transaction failures
    - _Requirements: 1.2, 3.3, 5.3, 5.4_

- [x] 3. Checkpoint - Ensure backend tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Add auto-assign API endpoint
  - [x] 4.1 Add AutoAssignDocuments endpoint to TransactionsController
    - Add POST endpoint at `/api/transactions/auto-assign`
    - Accept yearMonth query parameter
    - Call TransactionService.AutoAssignDocumentsAsync
    - Return AutoAssignResult
    - Handle exceptions and return appropriate HTTP status codes
    - _Requirements: 1.1, 4.1, 4.2, 4.3, 5.1_

  - [ ]* 4.2 Write unit tests for AutoAssignDocuments endpoint
    - Test successful auto-assignment
    - Test error handling
    - Test cancellation
    - _Requirements: 5.1_

- [x] 5. Implement frontend auto-assign button
  - [x] 5.1 Add auto-assign state and methods to TransactionsComponent
    - Add isAutoAssigning boolean property
    - Add autoAssignResult property
    - Implement autoAssignDocuments() method to call API
    - Implement dismissAutoAssignResult() method
    - Add hasUnmatchedTransactions computed property
    - Add getAutoAssignTooltip() method for button tooltip
    - _Requirements: 1.1, 1.2, 1.4, 1.5, 4.5, 6.1, 6.2, 6.3, 6.5_

  - [x] 5.2 Add auto-assign button to transactions template
    - Add Material button with icon in toolbar area
    - Bind to autoAssignDocuments() click handler
    - Bind disabled state to isAutoAssigning and hasUnmatchedTransactions
    - Add loading spinner when isAutoAssigning is true
    - Add tooltip with getAutoAssignTooltip()
    - _Requirements: 1.1, 1.4, 6.1, 6.2, 6.3, 6.5_

  - [x] 5.3 Add results dialog to transactions template
    - Create dialog div that shows when autoAssignResult is not null
    - Display totalProcessed, assignedCount, skippedCount
    - Display errors list if present
    - Add close button that calls dismissAutoAssignResult()
    - _Requirements: 4.1, 4.2, 4.3, 4.5_

  - [ ]* 5.4 Write property test for button state logic
    - **Property 7: Button state based on unmatched transactions**
    - **Validates: Requirements 6.4**

  - [ ]* 5.5 Write unit tests for TransactionsComponent auto-assign functionality
    - Test button rendering
    - Test button disabled state when no unmatched transactions
    - Test loading indicator display during operation
    - Test results dialog display and dismissal
    - Test grid refresh after completion
    - Test navigation cancellation
    - _Requirements: 1.1, 1.4, 1.5, 4.4, 4.5, 5.3, 6.1, 6.2, 6.3_

- [x] 6. Add styling for auto-assign UI components
  - Add CSS for auto-assign button
  - Add CSS for results dialog
  - Add CSS for loading spinner
  - Ensure responsive design
  - _Requirements: 1.1, 4.1_

- [x] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Integration testing
  - [x] 8.1 Test end-to-end auto-assignment flow
    - Create test transactions and documents
    - Click auto-assign button
    - Verify documents are assigned correctly
    - Verify results summary is accurate
    - _Requirements: 1.1, 1.2, 1.5, 2.2, 4.1, 4.2, 4.3_

  - [x] 8.2 Test error scenarios
    - Test with no available documents
    - Test with all matches below threshold
    - Test with network interruption
    - _Requirements: 2.2, 5.1_

- [x] 9. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- The feature leverages existing DocumentMatchingService for matching logic
- All database updates are performed in a single transaction for atomicity
