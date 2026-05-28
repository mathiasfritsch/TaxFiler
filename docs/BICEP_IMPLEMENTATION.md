# Bicep Infrastructure as Code Implementation Summary

This document summarizes the Infrastructure as Code implementation for TaxFiler using Bicep.

## What Was Implemented

### 1. Infrastructure Templates (infrastructure/)

#### Main Orchestration
- **main.bicep** - Central orchestration template that coordinates all module deployments
  - Manages parameter passing between modules
  - Provides unified output interface
  - Handles environment-specific configuration

#### Reusable Modules
- **modules/web-app-config.bicep** - Configures Azure App Service
  - Sets application settings (EntraID, logging, LlamaParse)
  - Configures authentication settings
  - Integrates with Key Vault for secrets

- **modules/key-vault.bicep** - Manages Azure Key Vault
  - Creates Key Vault for production secrets
  - Manages access policies
  - Grants Web App managed identity access

- **modules/entra-app-registration.bicep** - Manages EntraID app registration
  - Creates/updates EntraID application registration
  - Configures OAuth2 and API scopes
  - Creates service principals
  - Outputs important identifiers for configuration

#### Environment Parameters
- **parameters/prod.bicepparam** - Production environment settings
- **parameters/dev.bicepparam** - Development environment settings

### 2. GitHub Actions Integration

Updated `.github/workflows/azure-webapps-dotnet-core.yml`:
- Added checkout step to access Bicep templates
- Added Bicep validation step
- Added infrastructure deployment step before app deployment
- Integrated GitHub Secrets for credentials

### 3. Documentation

#### docs/GITHUB_SECRETS_SETUP.md
- Step-by-step guide for setting up GitHub Secrets
- Lists all required secrets
- Includes setup script
- Security best practices
- Troubleshooting guide

#### docs/DEPLOYMENT_GUIDE.md
- Comprehensive deployment instructions
- Prerequisites and quick start
- Local deployment steps
- Verification procedures
- Troubleshooting for common issues
- Post-deployment steps
- Monitoring and rollback strategies

#### docs/CONFIGURATION_GUIDE.md
- Application configuration overview
- Environment-specific configuration
- Key Vault integration details
- Configuration validation
- Best practices

## Key Features

### 1. Automated Infrastructure Deployment
- Bicep templates define all infrastructure as code
- GitHub Actions automatically deploys on push to main
- Reproducible deployments across environments

### 2. EntraID App Registration Management
- Automated creation and configuration of EntraID app
- OAuth2 and PKCE flow configuration
- Redirect URIs automatically set from parameters
- Service principal creation and management

### 3. Secure Secret Management
- GitHub Secrets for sensitive credentials
- Azure Key Vault for runtime secrets
- Key Vault references in app settings
- Managed identity authentication

### 4. Environment Separation
- Production and development parameter files
- Different configuration for each environment
- Consistent infrastructure definition

### 5. Modular Design
- Reusable Bicep modules for different components
- Easy to add new modules or modify existing ones
- Clear separation of concerns

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 GitHub Actions Workflow                     │
│  1. Checkout code                                           │
│  2. Build and test application                              │
│  3. Validate Bicep templates                                │
│  4. Deploy infrastructure (Bicep)                           │
│  5. Deploy application to App Service                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Azure Infrastructure                      │
├─────────────────────────────────────────────────────────────┤
│ Resource Group: TaxFiler                                    │
│ ├─ App Service: TaxFiler                                   │
│ │  ├─ Managed Identity                                     │
│ │  └─ Application Settings (from Bicep)                    │
│ │     ├─ EntraID configuration                             │
│ │     ├─ Key Vault references                              │
│ │     └─ Application settings                              │
│ ├─ Key Vault: taxfiler-kv-prod                            │
│ │  ├─ EntraID Client Secret                               │
│ │  ├─ Google Drive Client Secret                          │
│ │  └─ LlamaParse API Key                                  │
│ └─ EntraID App Registration (managed by Bicep)            │
│    ├─ Redirect URIs                                        │
│    ├─ API Scopes                                           │
│    └─ Service Principal                                    │
└─────────────────────────────────────────────────────────────┘
```

## Deployment Flow

### First Time Deployment

1. **Setup GitHub Secrets**
   - Follow docs/GITHUB_SECRETS_SETUP.md
   - Configure Azure credentials
   - Configure EntraID credentials

2. **Create Azure Resources** (manual first time)
   ```bash
   az group create --name TaxFiler --location EastUS
   az webapp create --resource-group TaxFiler --plan TaxFilerPlan --name TaxFiler
   ```

3. **Run GitHub Actions**
   - Push to main branch
   - Workflow automatically validates and deploys infrastructure
   - Application is deployed with configured settings

4. **Verify**
   - Check Azure Portal for resources
   - Test application authentication

### Subsequent Deployments

1. **Code Changes**
   - Modify application code
   - Modify Bicep templates (if infrastructure changes needed)

2. **Push to main**
   - GitHub Actions validates changes
   - Bicep templates are validated
   - Infrastructure is deployed (if templates changed)
   - Application is deployed

3. **Automatic**
   - No manual steps required
   - All deployments are auditable in Git history

## Configuration Flow

```
GitHub Secrets (ENTRA_CLIENT_SECRET, etc.)
           │
           ▼
GitHub Actions Workflow
           │
           ├─► Pass to Bicep deployment
           │
           ▼
Bicep Deployment
           │
           ├─► Create/Update App Service settings
           ├─► Configure Key Vault references
           ├─► Create/Update EntraID app
           │
           ▼
Azure App Service
           │
           ├─► appsettings.json (hardcoded defaults)
           ├─► App Service settings (from Bicep)
           ├─► Key Vault references (@Microsoft.KeyVault(...))
           ├─► Environment variables
           │
           ▼
Application Configuration
           │
           └─► Microsoft Identity Web reads EntraID config
               └─► Authentication works!
```

## Security Implementation

1. **Secret Handling**
   - Secrets stored in GitHub Secrets (not in code)
   - Passed to Bicep via parameters
   - Stored in Azure Key Vault
   - Not exposed in logs or outputs

2. **Access Control**
   - Service Principal with minimal permissions
   - Managed Identity for App Service
   - Key Vault access policies
   - RBAC for Azure resources

3. **Audit Trail**
   - All infrastructure changes in Git history
   - GitHub Actions logs for deployments
   - Azure deployment history
   - Application logs for runtime issues

## Next Steps

### 1. Setup GitHub Secrets (Required)

Follow docs/GITHUB_SECRETS_SETUP.md to:
- Create or obtain Azure service principal
- Set GitHub Secrets
- Test credentials

### 2. Deploy Infrastructure (First Time Manual)

```bash
# Create resource group
az group create --name TaxFiler --location EastUS

# Create app service (will be configured by Bicep)
az appservice plan create --resource-group TaxFiler --name TaxFilerPlan --sku F1
az webapp create --resource-group TaxFiler --plan TaxFilerPlan --name TaxFiler

# Trigger workflow
git push origin main
```

### 3. Verify Deployment

- Check GitHub Actions workflow runs
- Verify resources in Azure Portal
- Test application at https://taxfiler.azurewebsites.net

### 4. Store Additional Secrets

```bash
# Add to Key Vault
az keyvault secret set --vault-name taxfiler-kv-prod --name GoogleDriveClientSecret --value "<value>"
az keyvault secret set --vault-name taxfiler-kv-prod --name LlamaParseApiKey --value "<value>"
```

### 5. Update App Settings to Reference Key Vault

```bash
# Reference secrets from Key Vault
az webapp config appsettings set --resource-group TaxFiler --name TaxFiler --settings \
  "GoogleDriveSettings:ClientSecret=@Microsoft.KeyVault(SecretUri=https://taxfiler-kv-prod.vault.azure.net/secrets/GoogleDriveClientSecret/)" \
  "LlamaParse:ApiKey=@Microsoft.KeyVault(SecretUri=https://taxfiler-kv-prod.vault.azure.net/secrets/LlamaParseApiKey/)"
```

## Maintenance

### Regular Tasks

1. **Rotate Secrets** (quarterly or as needed)
   - Update GitHub Secrets
   - Update Key Vault secrets
   - Test authentication after rotation

2. **Review Infrastructure**
   - Check Bicep linting: `az bicep lint infrastructure/main.bicep`
   - Review Azure deployment history
   - Monitor resource costs

3. **Test Disaster Recovery**
   - Verify backup procedures
   - Test manual rollback
   - Document recovery procedures

### Updating Infrastructure

1. Modify Bicep templates
2. Validate locally: `az deployment group validate`
3. Push to main (triggers deployment)
4. Monitor GitHub Actions workflow
5. Verify in Azure Portal

## Troubleshooting

### Deployment Fails
- Check GitHub Actions logs
- Verify GitHub Secrets are set
- Validate Bicep: `az bicep lint infrastructure/main.bicep`
- Check Azure Portal for resources

### Application Can't Authenticate
- Verify EntraID configuration in App Service settings
- Check client ID and secret
- Verify redirect URIs in EntraID app registration
- Check Key Vault access policies

### Secrets Not Found
- Verify Key Vault name is correct
- Check managed identity has access
- Verify secrets exist in Key Vault
- Check Key Vault reference syntax

See docs/DEPLOYMENT_GUIDE.md for more troubleshooting steps.

## References

- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/)
- [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/)
- [EntraID Documentation](https://learn.microsoft.com/en-us/entra/)

## Questions or Issues?

Refer to the comprehensive documentation in the docs/ directory:
- docs/GITHUB_SECRETS_SETUP.md
- docs/DEPLOYMENT_GUIDE.md
- docs/CONFIGURATION_GUIDE.md
- infrastructure/README.md
