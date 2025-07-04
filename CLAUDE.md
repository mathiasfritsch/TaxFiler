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
- SQLite database for local development
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

## Document Processing

The LlamaIndex integration supports:
- PDF document parsing
- Invoice data extraction
- Tax-relevant information identification
- Structured data output in predefined formats (`Invoice`, `Tax` models)