# Implementation Plan: Multiple Document Attachments

## Overview

This implementation plan converts the multiple document attachments design into discrete coding tasks. The approach follows incremental development, starting with database schema changes, then service layer enhancements, API updates, and finally frontend modifications. Each task builds on previous work and includes validation through testing.

## Tasks

- [ ] 1. Database Schema and Migration Implementation
  - [x] 1.1 Create DocumentAttachment entity model
    - Create `TaxFiler.DB/Model/DocumentAttachment.cs` with all required properties
    - Add navigation properties and foreign key relationships
    - Include audit fields (AttachedAt, AttachedBy, IsAutomatic)
    - _Requirements: 1.1, 1.5_

  - [x] 1.2 Update Transaction and Document entities
    - Remove single DocumentId foreign key from Transaction entity
    - Add DocumentAttachments collection navigation property to Transaction
    - Add DocumentAttachments collection navigation property to Document
    - _Requirements: 1.1_

  - [x] 1.3 Update TaxFilerContext with new entity and relationships
    - Add DocumentAttachments DbSet to TaxFilerContext
    - Configure many-to-many relationship in OnModelCreating
    - Add unique constraint on TransactionId-DocumentId pairs
    - Configure cascade delete behavior
    - _Requirements: 1.1, 1.2, 1.3_

  - [ ]* 1.4 Write property test for database schema integrity
    - **Property 1: Database Schema Integrity**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5**

  - [x] 1.5 Create and test database migration
    - Generate migration using `dotnet ef migrations add AddMultipleDocumentAttachments`
    - Include data migration script to preserve existing single attachments
    - Add validation queries to ensure data integrity
    - Test migration on both PostgreSQL and SQLite
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ]* 1.6 Write property test for migration data preservation
    - **Property 2: Migration Data Preservation**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

- [ ] 2. Service Layer Implementation
  - [x] 2.1 Create DocumentAttachment DTOs
    - Create `TaxFiler.Model/Dto/AttachmentSummaryDto.cs`
    - Create `TaxFiler.Model/Dto/AttachDocumentRequestDto.cs`
    - Create `TaxFiler.Model/Dto/MultipleAssignmentResult.cs`
    - Update existing DTOs to support multiple attachments
    - _Requirements: 3.1, 3.6_

  - [x] 2.2 Implement IDocumentAttachmentService interface and service
    - Create `TaxFiler.Service/IDocumentAttachmentService.cs` interface
    - Implement `TaxFiler.Service/DocumentAttachmentService.cs`
    - Include methods for attach, detach, get attachments, and get summary
    - Use FluentResults for error handling
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [ ]* 2.3 Write property test for document attachment service
    - **Property 4: Document Attachment API Contract**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**

  - [x] 2.4 Enhance ReferenceMatcher for multiple voucher numbers
    - Add `ExtractVoucherNumbers` method to parse multiple references
    - Update `CalculateReferenceScore` to handle multiple document matching
    - Add support for common German voucher number patterns
    - _Requirements: 2.1_

  - [x] 2.5 Enhance AmountMatcher for multiple document amounts
    - Add `CalculateMultipleAmountScore` method for summing document amounts
    - Add `ValidateMultipleAmounts` method for amount validation
    - Handle Skonto calculations across multiple documents
    - _Requirements: 2.2, 2.3_

  - [x] 2.6 Update DocumentMatchingService for multiple documents
    - Add `FindMultipleDocumentMatchesAsync` method
    - Update `AutoAssignDocumentsAsync` to support multiple attachments
    - Add validation logic to prevent amount overages
    - Include audit logging for all matching decisions
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [ ]* 2.7 Write property test for multiple document matching
    - **Property 3: Multiple Document Matching**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**

- [x] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. API Layer Implementation
  - [x] 4.1 Create DocumentAttachmentsController
    - Create `TaxFiler.Server/Controllers/DocumentAttachmentsController.cs`
    - Implement endpoints for attach, detach, get attachments, get summary
    - Add auto-assign endpoint for multiple documents
    - Include proper authorization and error handling
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x] 4.2 Update TransactionDto and related mappers
    - Remove single DocumentId and Document properties from TransactionDto
    - Add AttachedDocuments collection and summary properties
    - Update TransactionMapper to handle multiple attachments
    - Include amount calculation logic for attached documents
    - _Requirements: 4.5, 4.6_

  - [ ]* 4.3 Write property test for amount calculation accuracy
    - **Property 5: Amount Calculation Accuracy**
    - **Validates: Requirements 4.5, 4.6**

  - [x] 4.4 Update TransactionsController for multiple attachments
    - Update existing endpoints to return multiple attachment data
    - Enhance auto-assign endpoint to support multiple documents
    - Add bulk operations for processing multiple transactions
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 4.5 Add validation and business rules
    - Implement duplicate attachment prevention
    - Add amount mismatch warnings
    - Include user permission validation
    - Add audit trail logging for all operations
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [ ]* 4.6 Write property test for business rule validation
    - **Property 6: Business Rule Validation**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5**

- [ ] 5. Performance Optimization
  - [x] 5.1 Optimize database queries for multiple attachments
    - Add database indexes for efficient querying
    - Implement eager loading for attachment data
    - Add pagination support for large attachment lists
    - Optimize DocumentMatchingService queries
    - _Requirements: 7.1, 7.2, 7.4_

  - [x] 5.2 Implement caching for frequently accessed data
    - Add caching for attachment summaries
    - Cache document matching results
    - Implement cache invalidation strategies
    - _Requirements: 7.5_

  - [ ]* 5.3 Write property test for performance and scalability
    - **Property 7: Performance and Scalability**
    - **Validates: Requirements 7.1, 7.2, 7.4, 7.5**

- [ ] 6. Service Registration and Configuration
  - [x] 6.1 Register new services in Program.cs
    - Add IDocumentAttachmentService registration
    - Update existing service registrations for enhanced functionality
    - Configure dependency injection for new components
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 6.2 Update database context registration
    - Ensure new entity is properly configured
    - Update connection string handling if needed
    - Test with both PostgreSQL and SQLite configurations
    - _Requirements: 1.1_

- [ ] 7. Integration Testing and Validation
  - [x] 7.1 Create integration tests for end-to-end scenarios
    - Test complete attachment workflow from API to database
    - Test migration process with realistic data
    - Test auto-assignment with complex transaction scenarios
    - _Requirements: 2.1, 2.2, 2.3, 6.1, 6.2_

  - [ ]* 7.2 Write unit tests for edge cases and error conditions
    - Test error handling for invalid inputs
    - Test boundary conditions for amount calculations
    - Test concurrent attachment operations
    - _Requirements: 3.4, 3.5, 3.6, 5.1, 5.2_

- [x] 8. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties using FsCheck.NUnit
- Unit tests validate specific examples and edge cases
- The implementation maintains backward compatibility with existing single document attachments
- All database changes include proper migration scripts and rollback capabilities