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

The application uses LlamaIndex Cloud for AI-powered document processing with a configured. 
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