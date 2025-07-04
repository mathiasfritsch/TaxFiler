# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

TaxFiler is a tax document processing application built as a full-stack solution with:
- **Backend**: ASP.NET Core 9.0 Web API with Entity Framework Core and SQLite
- **Frontend**: Angular 19 SPA with ag-Grid for data display
- **Document Processing**: LlamaIndex integration for AI-powered document parsing and tax information extraction
- **Authentication**: Microsoft Identity Web (EntraId/Azure AD)

## Project Structure

The solution follows a clean architecture pattern:

- **TaxFiler.Server** - Main web application hosting both API and SPA
- **TaxFiler.DB** - Entity Framework data layer with SQLite database
- **TaxFiler.Model** - DTOs and data transfer objects
- **TaxFiler.Service** - Business logic and external service integrations
- **TaxFiler.Service.Test** - Unit tests for service layer
- **taxfiler.client** - Angular frontend application
- **LlamaIndex.Core** - Custom LlamaIndex retriever implementation
- **LlamaParse** - Document parsing service integration

## Common Development Commands

### Building and Running
```bash
# Build entire solution
dotnet build

# Run the server (includes Angular dev server proxy)
dotnet run --project TaxFiler.Server

# Run Angular client separately
cd taxfiler.client
npm start
```

### Testing
```bash
# Run .NET tests
dotnet test

# Run Angular tests
cd taxfiler.client
npm test
```

### Database Management
```bash
# Create new migration
dotnet ef migrations add MigrationName --project TaxFiler.DB --startup-project TaxFiler.Server

# Update database
dotnet ef database update --project TaxFiler.DB --startup-project TaxFiler.Server
```

## Key Architectural Patterns

### Service Layer Architecture
- Services are registered via dependency injection in `Program.cs`
- Interface-based design with implementations in `TaxFiler.Service`
- Services handle business logic: `AccountService`, `DocumentService`, `TransactionService`, `SyncService`

### Document Processing Pipeline
1. **Upload**: Documents uploaded through `DocumentsController`
2. **Parse**: `ParseService` uses LlamaIndex to extract structured data
3. **Match**: `TransactionDocumentMatcherService` links documents to transactions
4. **Store**: Processed data stored in SQLite via Entity Framework

### External Integrations
- **LlamaIndex API**: Document parsing and extraction (`LlamaIndexService`)
- **Google Drive**: Document synchronization (`GoogleDriveService`)
- **Microsoft Identity**: Authentication and authorization

### Database Design
Core entities: `Document`, `Transaction`, `Account`, `TransactionDocumentMatcher`
- Uses Entity Framework migrations for schema management
- SQLite database for local development and in Production
- Automatic migration on application startup

## Configuration

### Required User Secrets
```bash
# Set up user secrets for development
dotnet user-secrets set "ConnectionStrings:TaxFilerDB" "Data Source=TaxfilerDb.db"
dotnet user-secrets set "GoogleDriveSettings:ClientId" "your-client-id"
dotnet user-secrets set "GoogleDriveSettings:ClientSecret" "your-client-secret"
```

### Angular Development
- Uses Angular CLI with proxy configuration for API calls
- Material Design components for UI
- ag-Grid for data tables with custom cell renderers

## Development Workflow

1. **Model First**: Create/modify entities in `TaxFiler.DB.Model`
2. **Create Migration**: Use EF Core migrations for schema changes
3. **Update Services**: Implement business logic in service layer
4. **API Controllers**: Expose endpoints in `TaxFiler.Server.Controllers`
5. **Frontend**: Update Angular components and services

## Authentication Flow

The application uses Microsoft Identity Web with EntraId:
- OAuth2 with PKCE flow
- Swagger UI includes authentication integration
- API endpoints secured with `[Authorize]` attributes

## LlamaIndex Document Processing

The application uses LlamaIndex Cloud for AI-powered document processing with a configured agent specialized for German tax documents. This integration allows for structured extraction of key invoice data such as amounts, dates, and tax rates.
The workflow involves multiple API calls in sequence:

### Processing Pipeline
1. **File Upload**: Documents are uploaded to LlamaIndex Cloud (`/api/v1/files`)
2. **Job Creation**: An extraction job is created using a preconfigured agent (`/api/v1/extraction/jobs`)
3. **Job Polling**: The system polls the job status until completion (`/api/v1/extraction/jobs/{jobId}`)
4. **Result Retrieval**: Extracted data is retrieved once processing is complete (`/api/v1/extraction/jobs/{jobId}/result`)

### Fixed Agent Configuration
- **Agent ID**: `d8494d42-5bd1-4052-b889-09eade1b740e` (configured in `appsettings.json`)
- **Purpose**: Specialized for German tax document processing
- **Project**: `c22f5d97-22f5-40ab-8992-e40e32b0992c`

### API Integration Details
- **Authentication**: Bearer token authentication via `LlamaBearerTokenHandler`
- **Service**: `LlamaIndexService` handles all API interactions
- **Timeout**: 5-minute default timeout with 5-second polling intervals
- **Error Handling**: Comprehensive error handling for job failures and timeouts

### Extracted Data Structure
The agent returns structured invoice data:
```csharp
public class InvoiceResult
{
    public string InvoiceNumber { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Total { get; set; }
    public decimal SubTotal { get; set; }
    public string InvoiceDate { get; set; } // German format
}
```

### Integration Flow
1. **ParseService** orchestrates the entire process
2. Documents are downloaded from Google Drive
3. Processed through LlamaIndex extraction
4. Results are parsed and stored in the database
5. Date parsing handles German locale (`de-DE`)

The fixed agent approach ensures consistent extraction quality for German tax documents without requiring per-document configuration.

## Document-Transaction Matching

The application provides manual document-to-transaction matching functionality through the UI, allowing users to link parsed documents with bank transactions for tax reporting purposes.

### Database Relationship
- **Transaction.DocumentId**: Foreign key linking transactions to documents
- **TransactionDocumentMatcher**: Configuration table for automated matching rules (currently not implemented)
- **Document.Unconnected**: Flag indicating whether a document is already linked to a transaction

### Manual Matching Process (UI)
1. **Transaction Grid View**: Navigate to `/transactions/{yearMonth}` to view transactions for a specific month
2. **Edit Transaction**: Click "Edit" button on any transaction to open the transaction edit dialog
3. **Document Selection**: Use the document autocomplete field to search and select a document
4. **Filtering Options**: 
   - **"nur unconnected" checkbox**: Toggle to show only unconnected documents vs. all documents
   - **Search by**: Document name, total amount, or invoice date
5. **Save**: Document is linked to transaction via `DocumentId` field

### UI Components for Matching
- **TransactionEditComponent**: Main dialog for editing transactions and linking documents
- **Document Autocomplete**: Material Design autocomplete with custom filtering
- **Smart Filtering**: Matches documents by name, total amount, or invoice date
- **Unconnected Toggle**: Filters to show only documents not yet linked to transactions

### Search and Filter Logic
The document autocomplete provides intelligent matching:
```typescript
// Filters documents by name, amount, or date
document.name.toLowerCase().includes(filterValue) ||
document.total == filterValueNumber ||
document.invoiceDate == filterValueDate
```

### Integration Points
- **GET /api/documents/getdocuments**: Loads all documents for selection
- **POST /api/transactions/updatetransaction**: Updates transaction with selected document ID
- **Document Display**: Shows document name in transaction grid after linking

### Automated Matching (Future)
The `TransactionDocumentMatcher` entity exists for future automated matching based on:
- **TransactionReceiver**: Counterparty name matching
- **TransactionCommentPattern**: Comment pattern matching
- **AmountMatches**: Amount validation
- Current implementation is incomplete (`throw new NotImplementedException()`)

### Best Practices
- Parse documents before attempting to match (ensures Total, InvoiceDate, etc. are populated)
- Use "unconnected only" filter to avoid duplicate assignments
- Match by amount first, then verify document details
- Review linked documents in transaction reports for accuracy

## Adding Transactions

The application supports adding transactions through CSV file upload and manual entry via the transaction-edit form component.

### Method 1: CSV File Upload

**Frontend Upload Process:**
1. **Navigation**: Go to `/transactions/{yearMonth}` page
2. **Upload Button**: Click "Upload" button in the transaction toolbar
3. **File Selection**: Choose CSV file using hidden file input
4. **Processing**: File is uploaded and parsed automatically
5. **Refresh**: Transaction grid refreshes to show new transactions

**CSV File Format (German Bank Export):**
The system expects German bank CSV exports with semicolon delimiters:

```csv
Bezeichnung Auftragskonto;IBAN Auftragskonto;BIC Auftragskonto;Bankname Auftragskonto;Buchungstag;Valutadatum;Name Zahlungsbeteiligter;IBAN Zahlungsbeteiligter;BIC (SWIFT-Code) Zahlungsbeteiligter;Buchungstext;Verwendungszweck;Betrag;Waehrung;Saldo nach Buchung;Bemerkung;Kategorie;Steuerrelevant;Glaeubiger ID;Mandatsreferenz
```

**Required CSV Columns:**
- **Buchungstag** → Transaction date (dd.MM.yyyy format)
- **Name Zahlungsbeteiligter** → Counterparty name
- **IBAN Zahlungsbeteiligter** → Counterparty IBAN  
- **Verwendungszweck** → Transaction comment/purpose
- **Betrag** → Amount (converted from cents to euros)

### Method 2: Manual Transaction Entry

**Angular Form Component:**
- **Component**: `TransactionEditComponent` can be used for both editing and adding
- **Form Fields**: All transaction properties (amount, date, counterparty, tax flags, etc.)
- **Document Linking**: Autocomplete field for linking documents during creation
- **Account Selection**: Dropdown for selecting the transaction account

**Transaction Form Fields:**
```typescript
{
  transactionNoteControl: FormControl,     // Comment/description
  netAmountControl: FormControl,           // Net amount
  grossAmountControl: FormControl,         // Gross amount  
  senderReceiverControl: FormControl,      // Counterparty name
  documentControl: FormControl,            // Linked document (optional)
  taxAmountControl: FormControl,           // Tax amount
  transactionDateTimeControl: FormControl, // Transaction date
  isSalesTaxRelevantControl: FormControl,  // Sales tax relevance flag
  isIncomeTaxRelevantControl: FormControl, // Income tax relevance flag
  accountControl: FormControl              // Account selection
}
```

### Backend Processing

**CSV Upload Pipeline:**
1. **Parsing**: Uses CsvHelper with semicolon delimiter and German culture
2. **Duplicate Detection**: Prevents re-importing existing transactions
3. **Data Transformation**: 
   - Direction detection (`IsOutgoing = Amount < 0`)
   - Amount normalization (convert to positive, set direction flag)
   - Default account assignment (`AccountId = 1`)

**Manual Entry Processing:**
- **API Endpoint**: `POST /api/transactions/updatetransaction`
- **Auto-calculation**: When document is linked, tax amounts auto-populate from document data
- **Skonto Handling**: Applies early payment discounts if document has skonto percentage

### API Endpoints

**Upload:** `POST /api/transactions/upload` (multipart/form-data)  
**Manual Add/Update:** `POST /api/transactions/updatetransaction`  
**Delete:** `DELETE /api/transactions/deleteTransaction/{id}`

### Duplicate Prevention

During CSV upload, transactions are skipped if matching records exist with identical:
- Transaction date
- Counterparty IBAN
- Comment
- Gross amount

### Transaction Export

**Download Reports:**
- **Endpoint**: `GET /api/transactions/download?yearMonth={yearMonth}`
- **Format**: CSV with comma delimiter
- **Filters**: Only tax-relevant transactions
- **Document Integration**: Includes linked document names with directional prefixes

## Document Management

The application supports two primary methods for managing documents: automatic synchronization from Google Drive and manual entry/editing through the UI.

### Method 1: Google Drive Synchronization

**Folder Structure Requirements:**
The Google Drive integration expects a specific folder hierarchy:
```
Google Drive/
├── 2025/           # Year folder
│   ├── 01/         # Month folder (01-12)
│   ├── 02/
│   └── ...
└── 2024/
    ├── 01/
    └── ...
```

**Synchronization Process:**
1. **Navigation**: Go to `/documents/{yearMonth}` page
2. **Sync Button**: Click "Sync from Google Drive" button
3. **Authentication**: Uses Google Service Account with Base64-encoded credentials
4. **Folder Traversal**: Finds year folder → month folder → retrieves files
5. **Database Update**: Creates new document records and marks orphaned documents

**Sync Logic (`SyncService`):**
- **New Files**: Creates `Document` records for files not in database
- **Orphaned Detection**: Marks existing documents as orphaned if no longer in Google Drive
- **File Filtering**: Only processes non-folder files (excludes Google Apps folder MIME type)
- **External Reference**: Stores Google Drive file ID as `ExternalRef`

**Google Drive Service Features:**
- **Authentication**: OAuth2 with service account credentials
- **Scopes**: Read-only access (`DriveService.Scope.DriveReadonly`)
- **File Operations**: List files, download file content
- **Folder Navigation**: Recursive folder ID lookup by name

### Method 2: Manual Document Entry/Editing

**Document Edit Component (`DocumentEditComponent`):**
- **Form Fields**: Complete document information editing
- **Usage**: Can edit existing documents or add new ones
- **API Integration**: Uses `POST /api/documents/updatedocument` and `POST /api/documents/adddocument`

**Editable Document Fields:**
```typescript
{
  nameControl: FormControl,           // Document name
  totalControl: FormControl,          // Gross amount (Brutto)
  subTotalControl: FormControl,       // Net amount (Netto)
  taxAmountControl: FormControl,      // Tax amount
  taxRateControl: FormControl,        // Tax rate percentage
  skontoControl: FormControl,         // Early payment discount
  invoiceDateControl: FormControl,    // Invoice date
  invoiceNumberControl: FormControl,  // Invoice number
  parsedControl: FormControl          // Parsed status checkbox
}
```

**Manual Entry Process:**
1. **Add Document**: Use `AddDocumentDto` for creating new documents
2. **Form Validation**: Name and ExternalRef are required fields
3. **Editing**: Click "Edit" button in documents grid to modify existing documents
4. **Save**: Form data is validated and posted to backend

### Document Processing Workflow

**From Google Drive:**
1. **Sync**: Files discovered in Google Drive folders
2. **Database Creation**: Document records created with Google Drive file ID
3. **Download**: Files downloaded on-demand for processing
4. **Parse**: LlamaIndex extracts structured data
5. **Link**: Documents linked to transactions manually

**Manual Entry:**
1. **Create**: Manual document entry with required fields
2. **Edit**: Modify document details through form interface
3. **Validation**: Backend validates required fields (Name, ExternalRef)
4. **Storage**: Document metadata stored in SQLite database

### API Endpoints

**Google Drive Operations:**
- **Sync**: `POST /api/documents/syncfiles/{yearMonth}`
- **Download**: `GET /api/documents/downloaddocument/{documentId}`

**Manual Document Operations:**
- **List**: `GET /api/documents/getdocuments`
- **Get**: `GET /api/documents/getdocument/{documentId}`
- **Add**: `POST /api/documents/adddocument`
- **Update**: `POST /api/documents/updatedocument`
- **Delete**: `DELETE /api/documents/deletedocument/{id}`
- **Parse**: `POST /api/documents/parse/{documentId}`

### Document Status Management

**Orphaned Documents:**
- **Detection**: Documents marked as orphaned when no longer found in Google Drive
- **Status**: `Orphaned` boolean flag tracks availability
- **Cleanup**: Manual cleanup required for orphaned documents

**Parsed Status:**
- **Flag**: `Parsed` boolean indicates if LlamaIndex has processed the document
- **Processing**: Parse button triggers LlamaIndex extraction
- **Data Population**: Successful parsing populates tax amounts, dates, and invoice numbers

**Connection Status:**
- **Unconnected**: Documents not yet linked to transactions
- **Connected**: Documents with active transaction relationships
- **Display**: Grid shows connection status for easy identification

### File Operations

**Download Functionality:**
- **Source**: Downloads from Google Drive using file ID
- **Format**: Returns PDF files with proper MIME types
- **Range Support**: Supports HTTP range requests for large files
- **Authentication**: Downloads require valid user authentication

**Delete Operations:**
- **Transaction Cleanup**: Removes document links from transactions before deletion
- **Cascade**: Sets `DocumentId` to null for linked transactions
- **Permanent**: Document records permanently removed from database