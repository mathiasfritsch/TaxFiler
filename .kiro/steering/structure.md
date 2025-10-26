# Project Structure

## Solution Organization

The TaxFiler solution follows a clean architecture pattern with clear separation of concerns across multiple projects.

## Project Structure

### Core Projects
- **TaxFiler.Server**: ASP.NET Core Web API and MVC host
- **taxfiler.client**: Angular frontend application
- **TaxFiler.DB**: Entity Framework Core data layer and migrations
- **TaxFiler.Model**: Shared data models, DTOs, and domain objects
- **TaxFiler.Service**: Business logic and service implementations

### Test Projects
- **TaxFiler.Service.Test**: Unit tests for service layer
- **TaxFiler.Predictor.Tests**: Tests for prediction/ML components
- **TaxFilerTests**: Additional test projects

### Supporting Projects
- **TaxFiler.Predictor**: Machine learning and prediction logic

## Folder Conventions

### Backend Structure
```
TaxFiler.Server/
├── Controllers/          # API controllers
├── Models/              # Request/response models
├── Mapper/              # Object mapping logic
├── Properties/          # Launch settings
├── Program.cs           # Application entry point
└── appsettings.json     # Configuration

TaxFiler.Service/
├── LlamaClient/         # External API clients
├── LlamaIndex/          # AI service integrations
├── I{Service}.cs        # Service interfaces
└── {Service}.cs         # Service implementations

TaxFiler.DB/
├── Model/               # Entity models
├── Migrations/          # EF Core migrations
└── TaxFilerContext.cs   # DbContext

TaxFiler.Model/
├── Dto/                 # Data transfer objects
├── Csv/                 # CSV import models
└── Llama/               # AI-related models
```

### Frontend Structure
```
taxfiler.client/src/
├── app/                 # Angular application
├── assets/              # Static assets
└── environments/        # Environment configurations
```

## Naming Conventions

### C# Projects
- **Interfaces**: Prefix with `I` (e.g., `IDocumentService`)
- **Services**: Suffix with `Service` (e.g., `DocumentService`)
- **DTOs**: Suffix with `Dto` (e.g., `DocumentDto`, `AddDocumentDto`)
- **Controllers**: Suffix with `Controller` (e.g., `DocumentController`)
- **Models**: Entity names without suffix (e.g., `Document`, `Transaction`)

### Database
- **Tables**: Plural entity names (e.g., `Documents`, `Transactions`)
- **Foreign Keys**: `{Entity}Id` (e.g., `DocumentId`)

### File Organization
- **One class per file**: Each class should have its own file
- **Folder by feature**: Group related files in feature folders
- **Separate interfaces**: Keep interfaces in same folder as implementations

## Architecture Patterns

### Dependency Injection
- Services registered in `Program.cs`
- Constructor injection throughout
- Interface-based abstractions

### Repository Pattern
- Entity Framework DbContext acts as repository
- Service layer provides business logic abstraction
- No additional repository layer

### Result Pattern
- Use `FluentResults.Result<T>` for error handling
- Avoid throwing exceptions for business logic errors
- Return success/failure results from services

### Configuration
- **Central Package Management**: All NuGet versions in `Directory.Packages.props`
- **Build Properties**: Shared settings in `Directory.Build.props`
- **User Secrets**: Sensitive configuration stored securely
- **Environment Variables**: Production configuration via environment