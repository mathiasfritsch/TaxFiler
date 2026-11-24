# TaxFiler

A document and transaction management system for managing and automatically assigning tax documents.

## Overview

TaxFiler is a full-stack web application designed to manage financial transactions and associated documents. The system provides intelligent automatic assignment of documents to transactions based on multiple criteria such as amount, date, vendor, and reference numbers.

##  Architecture

### Backend (.NET 10)
- **TaxFiler.Server**: ASP.NET Core Web API with Swagger/OpenAPI
- **TaxFiler.DB**: Entity Framework Core with PostgreSQL (NeonDB)
- **TaxFiler.Service**: Business logic and matching algorithms
- **TaxFiler.Model**: Data models and DTOs
- **TaxFiler.Service.Test**: Unit and integration tests

### Frontend (Angular 21)
- **taxfiler.client**: Angular Standalone Components with Material Design
- **AG Grid**: High-performance data grids for transactions and documents

## Key Features

### 1. Transaction Management
- Monthly view of transactions
- Inline editing with AG Grid
- CSV import from bank transactions
- Sales tax and income tax relevance flags
- Report export

### 2. Document Management
- Google Drive integration
- Document synchronization
- Document upload and management
- Metadata extraction (invoice number, date, amount)

### 3. Intelligent Document Assignment
The system uses a sophisticated multi-criteria matching algorithm:

#### Matching Criteria
- **Amount Matching**: With discount calculation (1%, 2%, 3%)
- **Date Matching**: Flexible tolerance (default: 7 days)
- **Vendor Matching**: String similarity (Levenshtein distance)
- **Reference Matching**: Invoice number matching

#### Weighted Scoring
```
Total Score = (Amount × 40%) + (Date × 30%) + (Vendor × 20%) + (Reference × 10%)
```

#### Features
- Direction-independent: Works for both incoming and outgoing transactions
- Configurable thresholds and weights
- Automatic batch assignment for entire months
- Intelligent error handling

### 4. Account Management
- Manage multiple bank accounts
- Account-specific transaction views
- CRUD operations for accounts

### 5. AI Integration (LlamaIndex)
- Integration with LlamaIndex Cloud API
- Intelligent document analysis
- Bearer Token Authentication

### 6. Document Parsing with LlamaIndex

The system uses LlamaIndex Cloud API to automatically extract structured data from PDF invoices and receipts.

#### Parsing Process

1. **File Upload**: Documents are downloaded from Google Drive and uploaded to LlamaIndex
2. **Job Creation**: An extraction agent job is created with the uploaded file
3. **Polling**: The system polls the job status every 5 seconds until completion (timeout: 300 seconds)
4. **Data Extraction**: Upon success, structured data is extracted and saved to the database

#### Extracted Fields

The following fields are automatically extracted from documents:

| Field | Type | Description |
|-------|------|-------------|
| **InvoiceNumber** | `string` | Invoice or receipt number |
| **InvoiceDate** | `DateOnly` | Date of the invoice (parsed with German culture format) |
| **SubTotal** | `decimal` | Net amount (before tax) |
| **Total** | `decimal` | Gross amount (including tax) |
| **TaxAmount** | `decimal` | Tax amount in currency |
| **TaxRate** | `decimal` | Tax rate as percentage (e.g., 19.0 for 19% VAT) |
| **Skonto** | `decimal?` | Early payment discount if available |
| **VendorName** | `string?` | Name of the merchant/vendor |

#### Implementation Details

**Service Architecture:**
- `ParseService`: Orchestrates the parsing workflow
- `LlamaIndexService`: Handles API communication with LlamaIndex Cloud
- `ILlamaApiClient`: Type-safe Refit HTTP client

**Configuration:**
```json
{
  "LlamaParse": {
    "AgentId": "your-agent-id"
  }
}
```

**API Workflow:**
```
1. POST /files/upload → Upload PDF file
2. POST /extraction/jobs → Create extraction job
3. GET /extraction/jobs/{id} → Poll job status
4. GET /extraction/jobs/{id}/result → Get extracted data
```

**Error Handling:**
- Job status monitoring (SUCCESS, ERROR, CANCELLED)
- Timeout handling for long-running jobs
- German date parsing with fallback
- Validation of required fields

**Example Usage:**
```csharp
var result = await parseService.ParseFilesAsync(documentId);
// Document fields are automatically updated and saved
```

The `Parsed` flag is set to `true` after successful extraction, allowing the system to track which documents have been processed.

## Technology Stack

### Backend
- **.NET 10**: Modern C# web development
- **ASP.NET Core**: RESTful API
- **Entity Framework Core**: ORM
- **PostgreSQL**: Relational database (NeonDB)
- **Swashbuckle**: Swagger/OpenAPI documentation
- **Microsoft Identity Web**: Entra ID Authentication
- **Refit**: Type-safe HTTP Client
- **xUnit**: Testing framework

### Frontend
- **Angular 21**: Modern web framework
- **Angular Material**: UI Component Library
- **AG Grid Community**: Enterprise-grade Data Grids
- **TypeScript 5**: Type-safe JavaScript
- **RxJS**: Reactive programming
- **Karma/Jasmine**: Testing

## Project Structure

```
TaxFiler/
├── TaxFiler.Server/          # ASP.NET Core Web API
│   ├── Controllers/          # API Endpoints
│   ├── Models/              # Request/Response Models
│   ├── Mapper/              # DTO Mapping
│   └── Program.cs           # Application Setup
├── TaxFiler.DB/             # Data Access Layer
│   ├── Model/               # Entity Models
│   ├── Migrations/          # EF Core Migrations
│   └── TaxFilerContext.cs   # DbContext
├── TaxFiler.Service/        # Business Logic
│   ├── DocumentMatchingService.cs
│   ├── AmountMatcher.cs
│   ├── DateMatcher.cs
│   ├── VendorMatcher.cs
│   ├── ReferenceMatcher.cs
│   ├── GoogleDriveService.cs
│   └── LlamaClient/         # AI Integration
├── TaxFiler.Model/          # Shared Models
│   ├── Dto/                 # Data Transfer Objects
│   ├── Csv/                 # CSV Models
│   └── Llama/               # AI Models
├── TaxFiler.Service.Test/   # Tests
│   ├── DocumentMatchingServiceIntegrationTests.cs
│   ├── AmountMatcherTests.cs
│   ├── DateMatcherTests.cs
│   └── SkontoIntegrationPerformanceTests.cs
└── taxfiler.client/         # Angular Frontend
    └── src/
        └── app/
            ├── transactions/
            ├── documents/
            ├── accounts/
            ├── transaction-edit/
            ├── document-edit/
            ├── auto-assign-result-dialog/
            └── shared/
```

##  Installation & Setup

### Prerequisites
- .NET 10 SDK
- Node.js 18+ and npm
- PostgreSQL database (or NeonDB account)
- Visual Studio 2022 or JetBrains Rider (optional)

### Backend Setup

1. **Clone the repository**
```bash
git clone <repository-url>
cd TaxFiler
```

2. **Configure database connection**
```bash
# Configure User Secrets
dotnet user-secrets init --project TaxFiler.Server
dotnet user-secrets set "ConnectionStrings:TaxFilerNeonDB" "Host=...;Database=...;Username=...;Password=..." --project TaxFiler.Server
```

Or set the environment variable:
```bash
set POSTGRESQLCONNSTR_TaxFilerNeonDB=Host=...;Database=...;Username=...;Password=...
```

3. **Run migrations**
```bash
cd TaxFiler.DB
dotnet ef database update
```

4. **Google Drive Settings (optional)**
```bash
dotnet user-secrets set "GoogleDriveSettings:FolderId" "<your-folder-id>" --project TaxFiler.Server
```

5. **LlamaIndex Configuration (optional)**
```bash
dotnet user-secrets set "LlamaParse:AgentId" "<your-agent-id>" --project TaxFiler.Server
dotnet user-secrets set "LlamaParse:ApiKey" "<your-api-key>" --project TaxFiler.Server
```

6. **Start the backend**
```bash
cd TaxFiler.Server
dotnet run
```

API runs on: `https://localhost:7142`

### Frontend Setup

1. **Install dependencies**
```bash
cd taxfiler.client
npm install
```

2. **Start development server**
```bash
npm start
```

Frontend runs on: `https://localhost:4200`

## Authentication

The project uses Microsoft Entra ID (Azure AD) for authentication:

- OAuth2 Authorization Code Flow
- Swagger UI supports OAuth2 login
- Configurable via `appsettings.json`

## Testing

### Run unit tests
```bash
cd TaxFiler.Service.Test
dotnet test
```

### Frontend tests
```bash
cd taxfiler.client
npm test
```

### Test Coverage
- Unit tests for all matching algorithms
- Integration tests for document matching
- Performance tests for discount calculations
- Backward compatibility tests

## API Endpoints

### Transactions
- `GET /api/transactions/gettransactions?yearMonth={yyyy-MM}&accountId={id}` - Get transactions
- `POST /api/transactions/updateTransaction` - Update transaction
- `POST /api/transactions/upload` - Import CSV
- `POST /api/transactions/auto-assign?yearMonth={yyyy-MM}` - Auto-assignment
- `DELETE /api/transactions/deleteTransaction/{id}` - Delete transaction
- `GET /api/transactions/download?yearMonth={yyyy-MM}` - Export report

### Documents
- `GET /api/documents/getdocuments?yearMonth={yyyy-MM}` - Get documents
- `POST /api/documents` - Create/update document
- `POST /api/documents/sync` - Google Drive sync
- `POST /api/documents/parse/{documentId}` - Parse document with LlamaIndex AI
- `DELETE /api/documents/{id}` - Delete document

### Accounts
- `GET /api/accounts` - Get all accounts
- `POST /api/accounts` - Create account
- `PUT /api/accounts/{id}` - Update account
- `DELETE /api/accounts/{id}` - Delete account

### Document Matching
- `GET /api/documentmatching/match/{transactionId}` - Find matches for transaction
- `POST /api/documentmatching/assign` - Assign document

## UI Features

### Material Design
- Consistent Material Design Components
- Responsive layout
- Material dialogs for edit operations
- Material buttons, forms, and inputs

### AG Grid Features
- Sorting and filtering
- Inline editing
- Custom cell renderer for buttons
- German localization
- Checkbox editors

### Navigation
- Monthly navigation (Previous/Next)
- Account filter via query parameters
- File upload integration
- Report download

## 🔧 Configuration

### Matching Configuration
```csharp
public class MatchingConfiguration
{
    public double AmountWeight { get; set; } = 0.4;      // 40%
    public double DateWeight { get; set; } = 0.3;        // 30%
    public double VendorWeight { get; set; } = 0.2;      // 20%
    public double ReferenceWeight { get; set; } = 0.1;   // 10%
    
    public double MinimumMatchScore { get; set; } = 0.7; // 70%
    public int DateToleranceDays { get; set; } = 7;
    public double MinimumVendorSimilarity { get; set; } = 0.6;
    
    public double[] SkontoPercentages { get; set; } = { 1.0, 2.0, 3.0 };
}
```

## Development Guidelines

### Code Style
- C#: Standard Microsoft Conventions
- TypeScript: ESLint + Prettier
- Standalone Components (no NgModules)
- New Angular Control Flow Syntax (`@if`, `@for`)

## Deployment

### Azure Web Apps
- CI/CD Pipeline in `.github/workflows/azure-webapps-dotnet-core.yml`
- Automatic deployment on push
- Environment variables for production

### Docker Support
- `docker-compose.yml` available
- Containerized deployment possible

## Additional Documentation

- **AGENTS.md**: Development notes and agent context
- **API Documentation**: Swagger UI at `/swagger`
- **Database Migrations**: `TaxFiler.DB/Migrations/`

## 🔗 Links

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Angular Documentation](https://angular.io/docs)
- [AG Grid Documentation](https://www.ag-grid.com/angular-data-grid/)
- [Material Design](https://material.angular.io/)

