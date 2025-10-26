# Technology Stack

## Backend (.NET)
- **.NET 9.0**: Target framework with latest C# language features
- **ASP.NET Core**: Web API and MVC framework
- **Entity Framework Core**: ORM with PostgreSQL and SQLite support
- **Microsoft Identity Web**: Azure AD/Entra ID authentication
- **Swagger/OpenAPI**: API documentation and testing
- **FluentResults**: Result pattern for error handling
- **Refit**: HTTP client library for external API calls

## Frontend (Angular)
- **Angular 19**: Frontend framework
- **Angular Material**: UI component library
- **AG-Grid**: Data grid component for tables
- **TypeScript 5.8**: Type-safe JavaScript
- **RxJS**: Reactive programming

## Database
- **PostgreSQL**: Primary database (production)
- **SQLite**: Development/testing database
- **Entity Framework Migrations**: Database schema management

## External Integrations
- **Google Drive API**: Document synchronization
- **LlamaIndex Cloud**: AI document processing
- **Azure AD/Entra ID**: Authentication and authorization

## Development Tools
- **Docker Compose**: Local PostgreSQL development
- **Central Package Management**: Unified NuGet package versions
- **User Secrets**: Secure configuration management
- **GitHub Actions**: CI/CD pipeline

## Common Commands

### Build and Run
```bash
# Build entire solution
dotnet build

# Run server (from TaxFiler.Server directory)
dotnet run

# Run client (from taxfiler.client directory)
npm start

# Run with Docker Compose (database)
docker-compose up -d
```

### Database Operations
```bash
# Add migration
dotnet ef migrations add <MigrationName> --project TaxFiler.DB

# Update database
dotnet ef database update --project TaxFiler.DB

# Drop database
dotnet ef database drop --project TaxFiler.DB
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test TaxFiler.Service.Test
```

### Client Development
```bash
# Install dependencies
npm install

# Development server with HTTPS
npm start

# Build for production
npm run build

# Run tests
npm test
```