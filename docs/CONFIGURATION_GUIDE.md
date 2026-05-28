# Application Configuration Guide

This document explains how TaxFiler's application configuration works with Bicep infrastructure management.

## Overview

TaxFiler uses a layered configuration approach:

1. **appsettings.json** - Default configuration (no secrets)
2. **appsettings.Development.json** - Local development overrides
3. **User Secrets** - Local development sensitive values
4. **Azure Key Vault** - Production secrets (runtime)
5. **App Service Application Settings** - Deployed configuration
6. **Environment Variables** - Runtime overrides

## Configuration Sources by Environment

### Local Development

For local development, use user secrets:

```bash
cd TaxFiler.Server

# Set up local user secrets
dotnet user-secrets set "ConnectionStrings:TaxFilerDB" "Data Source=TaxfilerDb.db"
dotnet user-secrets set "EntraId:ClientId" "533594db-31dc-4f88-83e9-b9c0f3a47922"
dotnet user-secrets set "EntraId:ClientSecret" "your-local-secret"
dotnet user-secrets set "GoogleDriveSettings:ClientId" "your-client-id"
dotnet user-secrets set "GoogleDriveSettings:ClientSecret" "your-client-secret"
dotnet user-secrets set "LlamaParse:ApiKey" "your-api-key"
```

The application automatically loads user secrets when running in Development environment.

### Production (Azure App Service)

Configuration is managed through:

1. **Bicep Deployment** - Sets initial app settings
2. **Key Vault References** - Runtime secret retrieval
3. **GitHub Actions** - Deploys with automated configuration

## Configuration Structure

### EntraID Configuration

```json
{
  "EntraId": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "https://taxfiler.azurewebsites.net/",
    "TenantId": "b925eae5-3023-4f8d-8414-8c56b7cee858",
    "ClientId": "533594db-31dc-4f88-83e9-b9c0f3a47922",
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://taxfiler-kv-prod.vault.azure.net/secrets/EntraClientSecret/)"
  }
}
```

### Application Settings

The following settings are managed by Bicep:

- `EntraId:Instance` - EntraID instance URL
- `EntraId:Domain` - Application domain
- `EntraId:TenantId` - Azure Tenant ID
- `EntraId:ClientId` - Application Client ID
- `EntraId:ClientSecret` - Application Client Secret (from Key Vault)
- `Logging:LogLevel:Default` - Default log level
- `Logging:LogLevel:Microsoft.AspNetCore` - ASP.NET Core log level
- `LlamaParse:AgentId` - LlamaParse agent ID

## Reading Configuration in Code

The application reads configuration from multiple sources via ASP.NET Core configuration providers:

```csharp
// In Program.cs
builder.Configuration.AddUserSecrets<Program>(); // Loads user secrets in development

// Access configuration
var config = builder.Configuration;
var entraConfig = config.GetSection("EntraId");
var clientId = entraConfig["ClientId"];
```

### Configuration in Controllers/Services

```csharp
public class SomeController : ControllerBase
{
    private readonly IConfiguration _config;

    public SomeController(IConfiguration config)
    {
        _config = config;
    }

    public IActionResult MyAction()
    {
        var value = _config["MyKey:SubKey"];
        return Ok(value);
    }
}
```

## Deploying Configuration Changes

### Adding New Configuration Values

1. **Update Bicep template** (`infrastructure/modules/web-app-config.bicep`):
   ```bicep
   properties: {
     'MyNewSetting': 'value'
   }
   ```

2. **Deploy infrastructure**:
   ```bash
   az deployment group create \
     --resource-group TaxFiler \
     --template-file infrastructure/main.bicep \
     --parameters infrastructure/parameters/prod.bicepparam
   ```

3. **Update application** to use the new setting

4. **Commit and merge** changes

### Adding Secrets to Key Vault

```bash
# Add secret to Key Vault
az keyvault secret set \
  --vault-name taxfiler-kv-prod \
  --name MySecretName \
  --value "secret-value"

# Reference in app settings using Key Vault reference
az webapp config appsettings set \
  --resource-group TaxFiler \
  --name TaxFiler \
  --settings "MySetting=@Microsoft.KeyVault(SecretUri=https://taxfiler-kv-prod.vault.azure.net/secrets/MySecretName/)"
```

## Key Vault Integration

The application connects to Key Vault at runtime using App Service Managed Identity:

1. **App Service Managed Identity** - Automatically authenticates to Key Vault
2. **Access Policy** - Grants Managed Identity read permissions to secrets
3. **Key Vault References** - App settings reference secrets like:
   ```
   @Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/{secret-name}/)
   ```

### Checking Key Vault Access

```bash
# Get App Service Managed Identity
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query principalId -o tsv)

# Check Key Vault access policy
az keyvault show \
  --resource-group TaxFiler \
  --name taxfiler-kv-prod \
  --query properties.accessPolicies
```

## Environment-Specific Configuration

Use separate parameter files for different environments:

### Development (dev.bicepparam)

```bicep
param environment = 'dev'
param keyVaultName = 'taxfiler-kv-dev'
param webAppDomain = 'taxfiler-dev.azurewebsites.net'
```

### Production (prod.bicepparam)

```bicep
param environment = 'prod'
param keyVaultName = 'taxfiler-kv-prod'
param webAppDomain = 'taxfiler.azurewebsites.net'
```

## Configuration Best Practices

1. **Never hardcode secrets** in code or configuration files
2. **Use Key Vault** for production secrets
3. **Use User Secrets** for local development
4. **Version infrastructure** code in Git
5. **Document configuration** changes in commit messages
6. **Test configuration** before deploying to production
7. **Rotate secrets** regularly (at least annually)
8. **Use appropriate permissions** (principle of least privilege)

## Troubleshooting Configuration Issues

### Application can't connect to Key Vault

```bash
# Check if Managed Identity is enabled
az webapp identity show --resource-group TaxFiler --name TaxFiler

# Check Key Vault access policy
az keyvault show \
  --resource-group TaxFiler \
  --name taxfiler-kv-prod \
  --query properties.accessPolicies
```

### Configuration value is null

1. Check if setting is defined in appsettings.json
2. Check if environment variable is set
3. Check if User Secret is configured (local development)
4. Verify Key Vault reference syntax

### EntraID authentication fails

1. Verify `EntraId:ClientId` and `EntraId:ClientSecret` are correct
2. Check `EntraId:TenantId` matches your Azure Tenant
3. Verify application redirect URIs are correctly configured
4. Check application permissions in EntraID

## Configuration Validation

To validate configuration is correctly deployed:

```bash
# List all app settings
az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler

# Check specific setting
az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query "[?name=='EntraId:ClientId']"

# Restart app to reload configuration
az webapp restart --resource-group TaxFiler --name TaxFiler
```

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/azure/azure-app-service/app-service-key-vault-references)
- [Manage App Service Configuration](https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings)
- [User Secrets in .NET](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
