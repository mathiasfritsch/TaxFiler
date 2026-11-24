# AGENTS.md

This file provides guidance to AI coding agents when working in this repository.

## Overview

TaxFiler is a full-stack tax document and transaction management application with:

- **Backend**: ASP.NET Core Web API (`net11.0`)
- **Frontend**: Angular 22 SPA with Angular Material and AG Grid
- **Database**: Entity Framework Core with PostgreSQL (NeonDB)
- **Auth**: Microsoft Identity Web (Entra ID)
- **AI parsing**: LlamaIndex Cloud integration via Refit

## Solution Structure

- `TaxFiler.Server/` - Web host, API controllers, Swagger, auth setup
- `TaxFiler.DB/` - EF Core DbContext, entities, migrations
- `TaxFiler.Model/` - DTOs and shared models
- `TaxFiler.Service/` - business logic and integrations (matching, Google Drive, LlamaIndex)
- `TaxFiler.Service.Test/` - NUnit tests for service layer and matching logic
- `taxfiler.client/` - Angular frontend
- `Http/` - HTTP request examples
- `Scripts/` - setup scripts

## Runtime and Tooling (current)

- .NET SDK pinned in `global.json`: **11.0.100**
- Shared target framework in `Directory.Build.props`: **net11.0**
- Central package management: enabled via `Directory.Packages.props`
- Angular dependencies: **22.x**

## Common Commands

### Build

```bash
dotnet build
```

### Run backend (with SPA proxy in development)

```bash
dotnet run --project TaxFiler.Server
```

### Run frontend directly

```bash
cd taxfiler.client
npm start
```

### Tests

```bash
dotnet test
```

Frontend e2e:

```bash
cd taxfiler.client
npm run test:e2e
```

### EF Core migrations

Create migration:

```bash
dotnet ef migrations add <MigrationName> --project TaxFiler.DB --startup-project TaxFiler.Server
```

Update database:

```bash
dotnet ef database update --project TaxFiler.DB --startup-project TaxFiler.Server
```

## Architecture Notes

### Service Registration

Dependency injection is wired in `TaxFiler.Server/Program.cs`, including:

- Core services (`ITransactionService`, `IDocumentService`, `IParseService`, etc.)
- Document matching services (`IAmountMatcher`, `IDateMatcher`, `IVendorMatcher`, `IReferenceMatcher`)
- LlamaIndex Refit client (`ILlamaApiClient`) with `LlamaBearerTokenHandler`

### Database

`TaxFilerContext` currently uses PostgreSQL (`UseNpgsql`) and checks:

1. `POSTGRESQLCONNSTR_TaxFilerNeonDB` environment variable
2. `ConnectionStrings:TaxFilerNeonDB`

Automatic migration is executed at application startup in `Program.Main`.

### Document Processing Flow

1. Documents are synced/uploaded and stored.
2. `ParseService` orchestrates LlamaIndex extraction.
3. `LlamaIndexService` calls LlamaIndex Cloud through `ILlamaApiClient` (Refit).
4. Extracted values are mapped to document fields.
5. Matching/assignment logic links documents to transactions.

### Matching System

Matching uses weighted scoring across:

- amount
- date
- vendor similarity
- reference similarity

Configuration is provided by `MatchingConfiguration`.

## Configuration

### Required user secrets (typical local setup)

```bash
dotnet user-secrets set "ConnectionStrings:TaxFilerNeonDB" "<postgres-connection-string>" --project TaxFiler.Server
dotnet user-secrets set "GoogleDriveSettings:ClientId" "<client-id>" --project TaxFiler.Server
dotnet user-secrets set "GoogleDriveSettings:ClientSecret" "<client-secret>" --project TaxFiler.Server
dotnet user-secrets set "LlamaParse:ApiKey" "<llama-api-key>" --project TaxFiler.Server
dotnet user-secrets set "LlamaParse:AgentId" "<agent-id>" --project TaxFiler.Server
```

## Working Guidelines for Agents

1. Prefer targeted, minimal changes that preserve current behavior.
2. Follow existing patterns in each project (DI style, DTO mapping, matcher structure).
3. When changing data models, keep EF migrations in sync.
4. When changing matching logic, run relevant tests in `TaxFiler.Service.Test`.
5. Keep API contracts and Angular client usage aligned.
6. For LlamaIndex integration changes, prefer extending Refit contracts over ad-hoc `HttpClient` code.

