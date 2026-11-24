---
title: Multiple Documents Per Transaction Support
version: 1.0
date_created: 2026-10-10
last_updated: 2026-10-10
owner: TaxFiler Development Team
tags: [data, schema, architecture, design, transaction, document, relationship]
---

# Introduction

This specification defines the requirements and design for enabling a single financial transaction to be associated with multiple supporting documents in the TaxFiler application. The current data model maintains a one-to-one relationship between transactions and documents via `Transaction.DocumentId`. This does not reflect real-world scenarios in which a single bank transaction represents the combined payment of several invoices, receipts, or credit notes.

The specification covers the database schema change, the service and API layer updates, the data migration strategy, and the acceptance and validation criteria required to deliver the feature safely while preserving existing data.

## 1. Purpose & Scope

### Purpose
Enable the TaxFiler system to model and manage cases where zero or more documents support a single financial transaction, replacing the current one-to-one `Transaction.DocumentId` association.

### Scope

**In Scope:**
- Database schema changes to support a one-to-many transaction-document relationship
- API and service layer updates for managing document collections
- Data migration from the existing one-to-one `Transaction.DocumentId` association
- Enhancements to the document matching service for multi-document scenarios
- Backward-compatibility handling during a transition period

**Out of Scope:**
- Frontend (Angular) user interface implementation details
- Many-to-many transaction-document relationships (one document across multiple transactions)
- Document merging, splitting, or OCR re-parsing logic
- Automated business-rule-driven document grouping

### Intended Audience
- Backend developers implementing service and data layer changes
- Database engineers managing EF Core migrations
- Frontend developers consuming the updated APIs
- QA engineers designing test scenarios
- Generative AI systems implementing the specification

### Assumptions
- Entity Framework Core is used for object-relational mapping and migrations
- PostgreSQL is the primary database (Npgsql provider)
- The target framework is .NET 10.0
- The existing document matching service (`DocumentMatchingService`) provides foundational matching logic
- Breaking schema changes are acceptable when accompanied by a reversible migration
- The application exposes RESTful API endpoints consumed by the Angular client

## 2. Definitions

- **Transaction**: A financial transaction record representing a money movement (payment or receipt) with amount, date, counterparty, and reference attributes.
- **Document**: A tax-relevant digital artifact (invoice, receipt, credit note) with parsed metadata including amounts, dates, vendor, and invoice number.
- **Transaction-Document Association**: A linkage between a transaction and a document that supports or evidences that transaction.
- **Junction Table**: An intermediary database table implementing a one-to-many or many-to-many relationship between two entities, optionally carrying association metadata.
- **Primary Document**: The first document in a transaction's collection, historically referenced by `Transaction.DocumentId`.
- **Supporting Document**: Any additional document beyond the primary that evidences the same transaction.
- **Orphaned Document**: A document not associated with any transaction.
- **Composite Transaction**: A single financial transaction representing payment for multiple documents.
- **Document Collection**: The ordered set of documents associated with a single transaction.
- **Match Score**: A numerical confidence value (0.0 to 1.0) indicating how well a document matches a transaction.
- **Sequence**: A positive integer defining the deterministic order of documents within a transaction's collection.
- **DocumentId**: The legacy nullable foreign key on `Transaction` that points to a single document.
- **TransactionDocuments**: The new junction table that implements the one-to-many relationship.

## 3. Requirements, Constraints & Guidelines

### Data Model Requirements

- **REQ-001**: The system SHALL support associating zero or more documents with a single transaction.
- **REQ-002**: The system SHALL enforce referential integrity between transactions and documents through a dedicated junction table.
- **REQ-003**: Each transaction-document association SHALL store a sequence number for deterministic ordering.
- **REQ-004**: Each transaction-document association SHALL store the timestamp at which the association was created.
- **REQ-005**: The system SHALL preserve a transaction-document association when document metadata changes.
- **REQ-006**: The system SHALL retain the legacy `Transaction.DocumentId` field during the transition period for backward compatibility.
- **REQ-007**: The legacy `DocumentId` and `Document` properties SHALL be marked obsolete once migration is complete.

### Business Logic Requirements

- **REQ-101**: When multiple documents are assigned to a transaction, the system SHALL maintain their assignment order via sequence numbers.
- **REQ-102**: The document matching service SHALL exclude documents already assigned to another transaction from candidate matches by default.
- **REQ-103**: A document SHALL be associated with at most one transaction at any given time.
- **REQ-104**: When calculating match scores, the system SHALL consider whether a document is already assigned.
- **REQ-105**: The system SHALL NOT hard-block assignment when the sum of document totals differs from the transaction gross amount; it SHALL surface an informational warning.
- **REQ-106**: Users SHALL be able to add documents to and remove documents from a transaction's document collection.
- **REQ-107**: The system SHALL support automatic document assignment based on match scores and availability.
- **REQ-108**: Automatic assignment SHALL assign the highest-scoring unassigned document first.

### API Requirements

- **REQ-201**: The transaction retrieval API SHALL include the complete, sequence-ordered collection of associated documents.
- **REQ-202**: Each document in the collection SHALL include its association metadata (sequence and timestamp).
- **REQ-203**: The system SHALL provide an endpoint to add a document to a transaction's collection.
- **REQ-204**: The system SHALL provide an endpoint to remove a document from a transaction's collection.
- **REQ-205**: The system SHALL provide an endpoint to reorder documents within a transaction's collection.
- **REQ-206**: The system SHALL provide an endpoint to list all documents for a transaction.
- **REQ-207**: Document assignment operations SHALL be atomic and transactional.
- **REQ-208**: The API SHALL return `409 Conflict` when assigning a document that is already associated with another transaction.

### Data Migration Requirements

- **REQ-301**: The migration SHALL preserve all existing transaction-document associations.
- **REQ-302**: Existing non-null `Transaction.DocumentId` values SHALL be transferred into the junction table.
- **REQ-303**: The migration SHALL be reversible to support rollback.
- **REQ-304**: After migration, the system SHALL validate that no associations were lost.
- **REQ-305**: The migration SHALL create indexes on foreign-key columns for query performance.

### Security Requirements

- **SEC-001**: All document association operations SHALL require an authenticated user.
- **SEC-002**: Users SHALL only modify transactions and documents belonging to accounts they are authorized to access.
- **SEC-003**: The system SHALL record document association changes for audit purposes.

### Performance Requirements

- **PER-001**: Retrieving a transaction with its document collection SHALL complete within 500 ms for collections of up to 20 documents.
- **PER-002**: The junction table SHALL be indexed on both foreign-key columns.
- **PER-003**: Bulk document assignment operations SHALL minimize database round trips.

### Constraints

- **CON-001**: A document SHALL NOT be assigned to multiple transactions simultaneously.
- **CON-002**: Sequence numbers within a transaction's document collection SHALL be unique.
- **CON-003**: Schema changes MUST remain backward compatible for one release cycle.
- **CON-004**: The solution MUST integrate with the existing EF Core migration infrastructure.

### Guidelines

- **GUD-001**: Prefer explicit sequence ordering over ordering by assignment time.
- **GUD-002**: Display documents in sequence order in all API responses and UI views.
- **GUD-003**: During the transition period, include both the legacy `DocumentId` (first document) and the `Documents` collection in API responses.
- **GUD-004**: Consider soft deletes for transaction-document associations to support audit trails.
- **GUD-005**: Document collections are typically small (1-5 documents); log a warning when a collection exceeds 10 documents.

### Patterns

- **PAT-001**: Use the Junction Table pattern to carry association metadata.
- **PAT-002**: Treat `Transaction` as the aggregate root with its documents as associated entities.
- **PAT-003**: Apply the `FluentResults.Result`/`Result<T>` pattern for service-layer outcomes.
- **PAT-004**: Use optimistic concurrency to prevent conflicting document assignments.

## 4. Interfaces & Data Contracts

### Database Schema

#### TransactionDocuments Junction Table

```sql
CREATE TABLE "TransactionDocuments" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "TransactionId" integer NOT NULL,
    "DocumentId" integer NOT NULL,
    "Sequence" integer NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    "AssignedBy" text NULL,
    "Notes" character varying(500) NULL,

    CONSTRAINT "FK_TransactionDocuments_Transactions_TransactionId"
        FOREIGN KEY ("TransactionId") REFERENCES "Transactions"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TransactionDocuments_Documents_DocumentId"
        FOREIGN KEY ("DocumentId") REFERENCES "Documents"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_TransactionDocuments_TransactionId_Sequence"
        UNIQUE ("TransactionId", "Sequence"),
    CONSTRAINT "UQ_TransactionDocuments_DocumentId"
        UNIQUE ("DocumentId")
);

CREATE INDEX "IX_TransactionDocuments_TransactionId"
    ON "TransactionDocuments" ("TransactionId");
CREATE INDEX "IX_TransactionDocuments_DocumentId"
    ON "TransactionDocuments" ("DocumentId");
```

#### Transaction Table

The existing `Transactions.DocumentId` column is retained as a legacy field. Its value is derived from the first document (sequence 1) in the associated collection and is removed in a future release.

### Entity Framework Models

#### TransactionDocument Entity

```csharp
namespace TaxFiler.DB.Model;

public class TransactionDocument
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Transaction))]
    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; }

    [ForeignKey(nameof(Document))]
    public int DocumentId { get; set; }
    public Document Document { get; set; }

    public int Sequence { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
```

#### Updated Transaction Entity

```csharp
namespace TaxFiler.DB.Model;

public class Transaction
{
    // ...existing properties...

    // New navigation property for multiple documents
    public ICollection<TransactionDocument> TransactionDocuments { get; set; }
        = new List<TransactionDocument>();

    // Legacy properties, retained for backward compatibility
    [Obsolete("Use TransactionDocuments collection instead.")]
    [ForeignKey(nameof(Document))]
    public int? DocumentId { get; set; }

    [Obsolete("Use TransactionDocuments collection instead.")]
    public Document? Document { get; set; }
}
```

#### Updated Document Entity

```csharp
namespace TaxFiler.DB.Model;

public class Document
{
    // ...existing properties...

    public ICollection<TransactionDocument> TransactionDocuments { get; set; }
        = new List<TransactionDocument>();
}
```

### API Data Transfer Objects

#### TransactionDto with Document Collection

```csharp
namespace TaxFiler.Model.Dto;

public class TransactionDto
{
    public int Id { get; set; }
    // ...existing properties...

    /// <summary>Documents associated with this transaction, ordered by sequence.</summary>
    public List<TransactionDocumentDto> Documents { get; set; } = new();

    /// <summary>Legacy field - ID of the first document in the collection.</summary>
    public int? DocumentId { get; set; }
}
```

#### TransactionDocumentDto

```csharp
namespace TaxFiler.Model.Dto;

public class TransactionDocumentDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int DocumentId { get; set; }
    public DocumentDto Document { get; set; }
    public int Sequence { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? Notes { get; set; }
}
```

#### AddDocumentToTransactionDto

```csharp
namespace TaxFiler.Model.Dto;

public class AddDocumentToTransactionDto
{
    [Required]
    public int DocumentId { get; set; }

    /// <summary>Optional; auto-assigned to the end of the collection when null.</summary>
    public int? Sequence { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
```

#### RemoveDocumentFromTransactionDto

```csharp
namespace TaxFiler.Model.Dto;

public class RemoveDocumentFromTransactionDto
{
    [Required]
    public int DocumentId { get; set; }
}
```

#### UpdateDocumentSequenceDto

```csharp
namespace TaxFiler.Model.Dto;

public class UpdateDocumentSequenceDto
{
    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int NewSequence { get; set; }
}
```

### API Endpoints

#### Get Transaction with Documents

```http
GET /api/transactions/{id}
Authorization: Bearer {token}

HTTP/1.1 200 OK
{
    "id": 123,
    "accountId": 1,
    "grossAmount": 1500.00,
    "transactionDateTime": "2026-01-15T10:30:00Z",
    "documents": [
        {
            "id": 1,
            "transactionId": 123,
            "documentId": 45,
            "sequence": 1,
            "assignedAt": "2026-01-15T12:00:00Z",
            "document": { "id": 45, "name": "Invoice_001.pdf", "total": 1000.00 }
        },
        {
            "id": 2,
            "transactionId": 123,
            "documentId": 46,
            "sequence": 2,
            "assignedAt": "2026-01-15T12:05:00Z",
            "document": { "id": 46, "name": "Invoice_002.pdf", "total": 500.00 }
        }
    ],
    "documentId": 45
}
```

#### Add Document to Transaction

```http
POST /api/transactions/{transactionId}/documents
Authorization: Bearer {token}
Content-Type: application/json

{ "documentId": 47, "sequence": 3, "notes": "Additional receipt" }

HTTP/1.1 201 Created
{
    "id": 3,
    "transactionId": 123,
    "documentId": 47,
    "sequence": 3,
    "assignedAt": "2026-10-10T14:30:00Z",
    "notes": "Additional receipt"
}

HTTP/1.1 409 Conflict
{ "error": "Document 47 is already assigned to transaction 456" }

HTTP/1.1 404 Not Found
{ "error": "Transaction 123 or Document 47 not found" }
```

#### Remove Document from Transaction

```http
DELETE /api/transactions/{transactionId}/documents/{documentId}
Authorization: Bearer {token}

HTTP/1.1 204 No Content

HTTP/1.1 404 Not Found
{ "error": "Document 47 is not associated with transaction 123" }
```

#### Update Document Sequence

```http
PUT /api/transactions/{transactionId}/documents/{documentId}/sequence
Authorization: Bearer {token}
Content-Type: application/json

{ "newSequence": 1 }

HTTP/1.1 200 OK
{ "id": 3, "transactionId": 123, "documentId": 47, "sequence": 1, "assignedAt": "2026-10-10T14:30:00Z" }
```

#### List Documents for Transaction

```http
GET /api/transactions/{transactionId}/documents
Authorization: Bearer {token}

HTTP/1.1 200 OK
[
    { "id": 1, "transactionId": 123, "documentId": 45, "sequence": 1, "document": { } },
    { "id": 2, "transactionId": 123, "documentId": 46, "sequence": 2, "document": { } }
]
```

### Service Layer Interface

```csharp
public interface ITransactionService
{
    // ...existing methods...

    Task<Result<TransactionDocumentDto>> AddDocumentToTransactionAsync(
        int transactionId,
        AddDocumentToTransactionDto addDto,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveDocumentFromTransactionAsync(
        int transactionId,
        int documentId,
        CancellationToken cancellationToken = default);

    Task<Result<TransactionDocumentDto>> UpdateDocumentSequenceAsync(
        int transactionId,
        int documentId,
        int newSequence,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TransactionDocumentDto>> GetTransactionDocumentsAsync(
        int transactionId,
        CancellationToken cancellationToken = default);
}
```

## 5. Acceptance Criteria

### Data Model Acceptance Criteria

- **AC-001**: Given a transaction with ID 123, When documents 45 and 46 are added, Then both are retrievable as part of the transaction's document collection.
- **AC-002**: Given document 45 assigned to transaction 123, When it is assigned to transaction 456, Then the system returns a conflict result.
- **AC-003**: Given documents 45, 46, 47 assigned to transaction 123 with sequences 1, 2, 3, When the transaction is retrieved, Then documents are returned in the order [45, 46, 47].
- **AC-004**: Given documents [45:1, 46:2, 47:3], When document 47 is moved to sequence 1, Then the result is [47:1, 45:2, 46:3].
- **AC-005**: Given transaction 123 with document 45, When transaction 123 is deleted, Then the association is cascade-deleted and document 45 remains in the Documents table.

### API Acceptance Criteria

- **AC-101**: Given a transaction ID, When `GET /api/transactions/{id}` is called, Then the response includes a `documents` array ordered by sequence.
- **AC-102**: Given a transaction and document ID, When `POST /api/transactions/{id}/documents` is called with valid data, Then `201 Created` is returned with the created association.
- **AC-103**: Given a document already assigned to transaction A, When `POST` assigns it to transaction B, Then `409 Conflict` is returned.
- **AC-104**: Given a transaction with document 45, When `DELETE /api/transactions/{id}/documents/45` is called, Then `204 No Content` is returned and the document no longer appears in the collection.
- **AC-105**: Given an authenticated user, When any transaction-document endpoint is called, Then the operation succeeds only if the user is authorized for the transaction's account.

### Migration Acceptance Criteria

- **AC-201**: Given transactions with existing `DocumentId` values, When the migration completes, Then all associations exist in `TransactionDocuments` with sequence 1.
- **AC-202**: Given a transaction with `DocumentId = 45`, When the migration completes, Then exactly one `TransactionDocuments` row exists with `DocumentId = 45` and `Sequence = 1`.
- **AC-203**: Given a transaction with `DocumentId = NULL`, When the migration completes, Then it has zero `TransactionDocuments` rows.
- **AC-204**: Given the migration completed, When a transaction is queried, Then the legacy `DocumentId` field returns the ID of the sequence-1 document.
- **AC-205**: Given all transactions, When the migration completes, Then the count of `TransactionDocuments` rows equals the count of non-null `Transaction.DocumentId` values before migration.

### Business Logic Acceptance Criteria

- **AC-301**: Given a transaction with gross amount 1500.00, When documents with totals [1000.00, 500.00] are assigned, Then the document total (1500.00) matches the transaction amount.
- **AC-302**: Given a transaction with gross amount 1500.00, When documents with totals [1000.00, 200.00] are assigned, Then the system surfaces a warning that the document total (1200.00) differs.
- **AC-303**: Given documents [45:1, 46:2, 47:3], When document 46 is removed, Then the remaining sequences are [45:1, 47:3].
- **AC-304**: Given an unassigned document with a high match score for transaction A and a low score for transaction B, When auto-assign runs, Then the document is assigned to transaction A.
- **AC-305**: Given a transaction with 10 associated documents, When an 11th is added, Then the operation succeeds and a warning is logged.

## 6. Test Automation Strategy

- **Test Levels**: Unit, Integration, End-to-End
- **Frameworks**: MSTest, FluentAssertions, Moq (for .NET applications)
- **Test Data Management**: Use builders/factories for consistent test data; each test creates and cleans up its own data; use EF Core InMemory provider for fast integration tests and PostgreSQL test containers for migration validation.
- **CI/CD Integration**: Tests run automatically on pull request and merge; migration tests run against a PostgreSQL instance; failing tests block merge.
- **Coverage Requirements**: Minimum 85% code coverage for the service layer.
- **Performance Testing**: Load-test 100 concurrent users; measure retrieval latency for 1, 5, 10, and 20 documents; benchmark auto-assign with 1000 transactions and 5000 documents.

### Example Test Structure

```csharp
[TestClass]
public class TransactionDocumentAssociationTests
{
    [TestMethod]
    public async Task AddDocumentToTransaction_ValidDocument_CreatesAssociation()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var document = CreateTestDocument();

        // Act
        var result = await _service.AddDocumentToTransactionAsync(
            transaction.Id,
            new AddDocumentToTransactionDto { DocumentId = document.Id });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentId.Should().Be(document.Id);
        result.Value.Sequence.Should().Be(1);
    }

    [TestMethod]
    public async Task AddDocumentToTransaction_AlreadyAssignedDocument_ReturnsConflict()
    {
        // Arrange
        var t1 = CreateTestTransaction();
        var t2 = CreateTestTransaction();
        var document = CreateTestDocument();
        await _service.AddDocumentToTransactionAsync(t1.Id,
            new AddDocumentToTransactionDto { DocumentId = document.Id });

        // Act
        var result = await _service.AddDocumentToTransactionAsync(t2.Id,
            new AddDocumentToTransactionDto { DocumentId = document.Id });

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("already assigned"));
    }
}
```

## 7. Rationale & Context

### Why One-to-Many Instead of Many-to-Many

The specification implements a one-to-many relationship (one transaction, many documents) rather than many-to-many because:

1. **Business domain**: A single payment transaction typically covers multiple invoices, while each invoice is expected to be settled by one transaction. Partial payments are a distinct scenario.
2. **Audit clarity**: A document belonging to exactly one transaction simplifies reconciliation, reporting, and tax compliance.
3. **Simplicity**: One-to-many is simpler to implement and reason about than many-to-many.
4. **Extensibility**: If many-to-many is later required, the junction table already exists and can be extended by dropping the unique constraint on `DocumentId`.

### Why a Junction Table

The junction table `TransactionDocuments` is used instead of a simple collection because it can carry association metadata (sequence, timestamp, assigner, notes), supports explicit changeable ordering, provides audit information, and is extensible for future attributes.

### Migration Strategy

The legacy `Transaction.DocumentId` field is retained during a transition period to preserve backward compatibility, enable incremental frontend updates, allow safe rollback, and provide time to validate the new implementation before full deprecation.

### Unique Document Constraint

The unique constraint on `DocumentId` in `TransactionDocuments` ensures a document is associated with at most one transaction, preventing duplicate claims, preserving reconciliation accuracy, and supporting tax compliance.

## 8. Dependencies & External Integrations

### External Systems
- **EXT-001**: Google Drive API - Documents are stored and retrieved from Google Drive; document identifiers reference external storage.

### Third-Party Services
- **SVC-001**: Authentication service - User identity and authorization for document association operations.
- **SVC-002**: Audit logging service - Association changes must be recorded for compliance.

### Infrastructure Dependencies
- **INF-001**: PostgreSQL database - Primary data store.
- **INF-002**: Entity Framework Core - ORM and migration management.
- **INF-003**: Database migration pipeline - Automated schema deployment via EF Core migrations.

### Data Dependencies
- **DAT-001**: Existing transaction data - Migration must preserve current `Transaction.DocumentId` associations.
- **DAT-002**: Existing document data - No document schema change is required, but document availability must be preserved.

### Technology Platform Dependencies
- **PLT-001**: .NET 10.0 runtime - Required for modern C# features and EF Core.
- **PLT-002**: ASP.NET Core - Web API framework for RESTful endpoints.
- **PLT-003**: FluentResults library - Consistent result pattern in the service layer.

### Compliance Dependencies
- **COM-001**: GDPR - Association data may contain personal data; audit logs must respect retention policies.
- **COM-002**: Tax authority regulations - Transaction-document relationships must support tax audits.

## 9. Examples & Edge Cases

### Example 1: Composite Payment

```csharp
// Transaction: single payment of 1500 EUR covering two invoices
var transaction = new Transaction
{
    Id = 100,
    GrossAmount = 1500.00m,
    TransactionDateTime = new DateTime(2026, 1, 15),
    Counterparty = "Office Supplies Inc",
    TransactionNote = "Payment for invoices #1234 and #1235"
};

var invoice1 = new Document { Id = 50, Total = 1000.00m, InvoiceNumber = "1234" };
var invoice2 = new Document { Id = 51, Total = 500.00m, InvoiceNumber = "1235" };

await service.AddDocumentToTransactionAsync(100, new AddDocumentToTransactionDto { DocumentId = 50, Sequence = 1 });
await service.AddDocumentToTransactionAsync(100, new AddDocumentToTransactionDto { DocumentId = 51, Sequence = 2 });

// Transaction 100 has documents [50:1, 51:2]; total (1500) matches gross amount.
```

### Example 2: Invoice with Credit Note

```csharp
// Payment of 900 EUR after a 100 EUR credit note against a 1000 EUR invoice
var transaction = new Transaction { Id = 101, GrossAmount = 900.00m };
var invoice = new Document { Id = 60, Total = 1000.00m };
var creditNote = new Document { Id = 61, Total = -100.00m };

await service.AddDocumentToTransactionAsync(101, new AddDocumentToTransactionDto { DocumentId = 60, Sequence = 1 });
await service.AddDocumentToTransactionAsync(101, new AddDocumentToTransactionDto { DocumentId = 61, Sequence = 2 });

// Document total (900) matches transaction amount (900).
```

### Edge Case 1: Document Already Assigned

```csharp
var r1 = await service.AddDocumentToTransactionAsync(100, new AddDocumentToTransactionDto { DocumentId = 50 });
// Success: document 50 assigned to transaction 100

var r2 = await service.AddDocumentToTransactionAsync(101, new AddDocumentToTransactionDto { DocumentId = 50 });
// Failure: "Document 50 is already assigned to transaction 100"
```

### Edge Case 2: Reordering

```csharp
// Initial: [50:1, 51:2, 52:3]
await service.UpdateDocumentSequenceAsync(100, 52, 1);
// Result: [52:1, 50:2, 51:3] (remaining sequences renumbered to preserve uniqueness)
```

### Edge Case 3: Amount Mismatch Warning

```csharp
// Transaction gross amount 1000; documents total 1200
await service.AddDocumentToTransactionAsync(t.Id, new AddDocumentToTransactionDto { DocumentId = 50 }); // 800
await service.AddDocumentToTransactionAsync(t.Id, new AddDocumentToTransactionDto { DocumentId = 51 }); // 400
// Association succeeds; warning logged: document total (1200) exceeds transaction amount (1000).
```

### Edge Case 4: Migration Preservation

```sql
-- Before migration
SELECT "Id", "DocumentId" FROM "Transactions" WHERE "Id" = 100;
-- => Id=100, DocumentId=50

-- After migration
SELECT * FROM "TransactionDocuments" WHERE "TransactionId" = 100;
-- => Id=1, TransactionId=100, DocumentId=50, Sequence=1

-- Legacy field still returns the first document
SELECT "Id", "DocumentId" FROM "Transactions" WHERE "Id" = 100;
-- => Id=100, DocumentId=50
```

### Edge Case 5: Orphaned Document Detection

```csharp
var isOrphaned = !await context.TransactionDocuments
    .AnyAsync(td => td.DocumentId == documentId);
// Orphaned documents can be assigned without conflict.
```

## 10. Validation Criteria

- **VAL-001**: `TransactionDocuments` exists with the specified columns, constraints, and indexes.
- **VAL-002**: Foreign keys enforce referential integrity to `Transactions` and `Documents`.
- **VAL-003**: Unique constraint on `DocumentId` prevents multi-transaction assignment.
- **VAL-004**: Unique constraint on `(TransactionId, Sequence)` prevents duplicate sequences.
- **VAL-005**: Cascade delete on `TransactionId` removes associations when a transaction is deleted.
- **VAL-101**: `TransactionDocument` entity maps to the junction table with all properties.
- **VAL-102**: `Transaction` exposes `TransactionDocuments` as `ICollection<TransactionDocument>`.
- **VAL-103**: `Document` exposes `TransactionDocuments` as `ICollection<TransactionDocument>`.
- **VAL-104**: EF Core migrations exist and apply forward and backward without data loss.
- **VAL-201**: New endpoints return correct status codes (200, 201, 204, 400, 404, 409).
- **VAL-202**: `GET /api/transactions/{id}` returns documents ordered by sequence.
- **VAL-203**: `POST` validates input and enforces business rules.
- **VAL-204**: `DELETE` removes the association without deleting the document.
- **VAL-205**: `PUT .../sequence` reorders and renumbers correctly.
- **VAL-206**: All endpoints require authentication and enforce authorization.
- **VAL-301**: `ITransactionService` includes the four new methods.
- **VAL-302**: `AddDocumentToTransactionAsync` enforces the unique document constraint.
- **VAL-303**: `RemoveDocumentFromTransactionAsync` does not affect other transactions.
- **VAL-304**: `UpdateDocumentSequenceAsync` renumbers to preserve uniqueness.
- **VAL-305**: `GetTransactionDocumentsAsync` returns documents in sequence order.
- **VAL-401**: All existing `Transaction.DocumentId` values transfer to `TransactionDocuments`.
- **VAL-402**: Migrated associations have `Sequence = 1` and a valid `AssignedAt`.
- **VAL-403**: Transactions with null `DocumentId` have zero junction rows.
- **VAL-404**: Count validation confirms no association loss.
- **VAL-405**: Legacy `DocumentId` returns the first document ID.
- **VAL-501**: The matching service correctly distinguishes orphaned from assigned documents.
- **VAL-502**: Auto-assign assigns the highest-scoring unassigned document first.
- **VAL-503**: Bulk operations handle multiple assignments atomically.
- **VAL-504**: Concurrent assignments are handled safely.
- **VAL-505**: Audit logging captures all association changes.
- **VAL-601**: Unit test coverage meets the 85% service-layer threshold.
- **VAL-602**: Integration tests cover constraints and cascade behavior.
- **VAL-603**: API tests cover success and error scenarios.
- **VAL-604**: Migration tests validate forward and backward migration.
- **VAL-605**: Performance tests meet the 500 ms retrieval target.
- **VAL-701**: OpenAPI documentation includes all new endpoints with examples.
- **VAL-702**: Schema documentation includes an ERD for the junction table.
- **VAL-703**: A migration guide documents the upgrade steps.
- **VAL-704**: Release notes describe the breaking change and `DocumentId` deprecation.

## 11. Related Specifications / Further Reading

- [TaxFiler Product Overview](file:///c:/projects/TaxFiler/.kiro/steering/product.md)
- [TaxFiler Technology Stack](file:///c:/projects/TaxFiler/.kiro/steering/tech.md)
- [TaxFiler Project Structure](file:///c:/projects/TaxFiler/.kiro/steering/structure.md)
- [Tax Mismatch Confirmation](file:///c:/projects/TaxFiler/TAX_MISMATCH_CONFIRMATION.md)

### Entity Framework Core Documentation
- [EF Core Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Many-to-Many Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)

### Database Design Patterns
- [Associative Entity (Junction Table)](https://en.wikipedia.org/wiki/Associative_entity)
- [Soft Delete Pattern](https://www.martinfowler.com/eaaCatalog/softDelete.html)

### API Design Guidelines
- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md)
- [HTTP Status Codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)

### Testing Resources
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/devlooped/moq)
- [Test Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html)
