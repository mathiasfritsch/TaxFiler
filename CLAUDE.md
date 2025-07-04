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

### Refit Integration

**Current State:**
The codebase now uses Refit for all LlamaIndex API calls, providing type safety and automatic serialization.

**Existing Refit Setup:**
```csharp
// Program.cs - Already configured
builder.Services.AddTransient<LlamaBearerTokenHandler>();
builder.Services
    .AddRefitClient<ILlamaApiClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.cloud.llamaindex.ai"))
    .AddHttpMessageHandler<LlamaBearerTokenHandler>();
```

**Complete ILlamaApiClient Interface:**
```csharp
public interface ILlamaApiClient
{
    [Multipart]
    [Post("/api/v1/files")]
    Task<LlamaIndexUploadFileResponse> UploadFileAsync([AliasAs("upload_file")] StreamPart file);

    [Post("/api/v1/extraction/jobs")]
    Task<LlamaIndexExtractionJobCreationResponse> CreateExtractionJobAsync([Body] object payload);

    [Get("/api/v1/extraction/jobs/{jobId}")]
    Task<LlamaIndexExtractionJobStatusResponse> GetExtractionJobAsync(string jobId);

    [Get("/api/v1/extraction/jobs/{jobId}/result")]
    Task<LlamaIndexJobResultResponse> GetExtractionJobResultAsync(string jobId);

    [Get("/api/v1/extraction/extraction-agents?project_id=c22f5d97-22f5-40ab-8992-e40e32b0992c")]
    Task<string> GetAgents();
}
```

**Implementation Details:**
- **LlamaIndexService**: Now injects `ILlamaApiClient` instead of creating `HttpClient`
- **Authentication**: `LlamaBearerTokenHandler` reads API key from `LlamaParse:ApiKey` configuration
- **Type Safety**: All API calls use strongly typed request/response models
- **Error Handling**: Refit provides consistent HTTP error handling

**Benefits Achieved:**
- **Type Safety**: Strongly typed request/response models eliminate manual JSON handling
- **Automatic Serialization**: JSON serialization/deserialization handled by Refit
- **Authentication**: Bearer token automatically added via `LlamaBearerTokenHandler` 
- **HTTP Configuration**: Base URL and timeouts centrally configured in DI
- **Error Handling**: Consistent HTTP error handling across all API calls
- **Testability**: Easy to mock `ILlamaApiClient` interface for unit testing
- **Maintainability**: Eliminates boilerplate HTTP client code

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

## Refactoring Tasks

### Service Integrations
- **Change LlamaIndexService to use Refit**: Migrate the current API integration to use Refit for type-safe HTTP calls
- Refactor API interactions to use Refit interfaces for better type safety and simpler HTTP request management
- Update dependency injection to register Refit-based service implementations

## Document-Transaction Matching

The application provides manual document-to-transaction matching functionality through the UI, allowing users to link parsed documents with bank transactions for tax reporting purposes.
