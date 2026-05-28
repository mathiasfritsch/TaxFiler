# TaxFiler Infrastructure as Code (Bicep)

This directory contains Bicep templates for managing TaxFiler's cloud infrastructure, with a focus on EntraID app registration and Azure App Service configuration.

## Overview

The infrastructure is organized as follows:

- **main.bicep** - Main orchestration template that coordinates all deployments
- **modules/** - Reusable Bicep modules for specific components:
  - `entra-app-registration.bicep` - Manages EntraID application registration
  - `web-app-config.bicep` - Configures Azure App Service settings
  - `key-vault.bicep` - Manages Azure Key Vault for secrets
- **parameters/** - Environment-specific parameter files:
  - `prod.bicepparam` - Production environment configuration
  - `dev.bicepparam` - Development environment configuration

## Prerequisites

1. Azure CLI installed and authenticated:
   ```bash
   az login
   az account set --subscription <SUBSCRIPTION_ID>
   ```

2. Appropriate permissions to:
   - Create/modify applications in EntraID
   - Create/modify resources in the target resource group
   - Manage Key Vault

## Deployment

### Local Development Deployment

1. **Validate templates:**
   ```bash
   az bicep lint infrastructure/main.bicep
   az deployment group validate \
     --resource-group <RESOURCE_GROUP_NAME> \
     --template-file infrastructure/main.bicep \
     --parameters infrastructure/parameters/dev.bicepparam \
     --parameters entraClientSecret='<YOUR_SECRET>'
   ```

2. **Deploy infrastructure:**
   ```bash
   az deployment group create \
     --resource-group <RESOURCE_GROUP_NAME> \
     --template-file infrastructure/main.bicep \
     --parameters infrastructure/parameters/dev.bicepparam \
     --parameters entraClientSecret='<YOUR_SECRET>' \
     --name taxfiler-infra-dev
   ```

### Production Deployment

The production deployment is handled automatically by GitHub Actions. See `.github/workflows/azure-webapps-dotnet-core.yml` for details.

## Parameters

### Required Parameters (from GitHub Secrets)

- **entraClientSecret** - EntraID application client secret (stored in GitHub Secrets)

### Optional Parameters

- **environment** - Deployment environment (default: 'prod')
- **location** - Azure region for resources (default: resource group location)
- **webAppPrincipalId** - Object ID of Web App managed identity for Key Vault access (optional)

## Key Features

### 1. EntraID App Registration Management

The Bicep templates manage EntraID app registration including:
- Application registration with OAuth2 configuration
- Redirect URIs for authentication flows
- API scopes and permissions
- Service principal creation

### 2. Secure Configuration Management

- Sensitive values (client secrets) are passed via parameters
- Azure Key Vault integration for runtime secret storage
- App Service authentication settings configuration

### 3. Environment Separation

- Separate parameter files for dev and prod environments
- Consistent infrastructure definition with environment-specific values
- Tag-based resource organization

## Configuration in Application

The Bicep templates configure the following application settings:

```
EntraId:Instance = https://login.microsoftonline.com/
EntraId:Domain = https://{webAppDomain}/
EntraId:TenantId = {tenantId}
EntraId:ClientId = {clientId}
EntraId:ClientSecret = {clientSecret}
```

These values are automatically injected into the Azure App Service configuration during deployment.

## Outputs

After deployment, the following values are available:

- `keyVaultUri` - URI of the deployed Key Vault
- `keyVaultId` - Resource ID of the deployed Key Vault
- `webAppConfigResourceId` - Resource ID of the app configuration
- `environment` - Deployed environment name
- `location` - Deployed region

## GitHub Actions Integration

The deployment is integrated into the GitHub Actions workflow:

1. Templates are validated during the workflow
2. Infrastructure is deployed before application deployment
3. Client secrets are retrieved from GitHub Secrets
4. Configuration is applied to the running App Service

## Adding New Parameters

To add new environment-specific parameters:

1. Define the parameter in `main.bicep`
2. Add to the respective `parameters/*.bicepparam` file
3. Update GitHub Actions secrets if needed
4. Update this README with parameter documentation

## Troubleshooting

### Deployment Fails with Permission Error

Ensure your Azure CLI account has sufficient permissions:
- Microsoft.Graph/applications write access
- Microsoft.KeyVault/vaults write access
- Microsoft.Web/sites write access

### EntraID App Already Exists

If the app already exists, the Bicep template will update the existing registration rather than create a new one.

### Key Vault Access Issues

Ensure the Web App's managed identity has been granted access to Key Vault:
```bash
az keyvault set-policy \
  --name <KEY_VAULT_NAME> \
  --object-id <WEB_APP_PRINCIPAL_ID> \
  --secret-permissions get list
```

## Best Practices

1. **Always validate before deploying:**
   ```bash
   az deployment group validate \
     --resource-group <RESOURCE_GROUP> \
     --template-file infrastructure/main.bicep \
     --parameters infrastructure/parameters/prod.bicepparam
   ```

2. **Use parameter files for environment-specific values**

3. **Store secrets in GitHub Secrets, not in parameter files**

4. **Test changes in dev environment first**

5. **Review Bicep linting results:**
   ```bash
   az bicep lint infrastructure/main.bicep
   ```

## References

- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Microsoft.Graph Application Resource](https://learn.microsoft.com/en-us/azure/templates/microsoft.graph/2020-03-01/applications)
- [Azure Key Vault Bicep Reference](https://learn.microsoft.com/en-us/azure/templates/microsoft.keyvault/vaults)
- [Web App Configuration Bicep Reference](https://learn.microsoft.com/en-us/azure/templates/microsoft.web/sites/config)
