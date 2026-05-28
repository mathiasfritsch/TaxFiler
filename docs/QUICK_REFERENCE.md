# TaxFiler Bicep Infrastructure - Quick Reference

## Directory Structure

```
infrastructure/
├── main.bicep                           # Main orchestration template
├── modules/
│   ├── entra-app-registration.bicep    # EntraID app management
│   ├── web-app-config.bicep            # App Service configuration
│   └── key-vault.bicep                 # Key Vault setup
├── parameters/
│   ├── prod.bicepparam                 # Production parameters
│   └── dev.bicepparam                  # Development parameters
└── README.md                            # Infrastructure documentation

docs/
├── BICEP_IMPLEMENTATION.md              # Implementation summary
├── GITHUB_SECRETS_SETUP.md              # GitHub Secrets configuration
├── DEPLOYMENT_GUIDE.md                  # Step-by-step deployment
└── CONFIGURATION_GUIDE.md               # Application configuration
```

## Quick Commands

### Local Development

```bash
# Validate templates
az bicep lint infrastructure/main.bicep

# Dry run (validate)
az deployment group validate \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/dev.bicepparam \
  --parameters entraClientSecret='<SECRET>'

# Deploy locally
az deployment group create \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/dev.bicepparam \
  --parameters entraClientSecret='<SECRET>'
```

### Production Deployment

Production deployment is automatic via GitHub Actions when you push to main.

```bash
# View deployment history
az deployment group list \
  --resource-group TaxFiler \
  --query "sort_by([].{name:name, timestamp:properties.timestamp}, &timestamp)"

# Get specific deployment details
az deployment group show \
  --resource-group TaxFiler \
  --name <DEPLOYMENT_NAME>
```

## GitHub Secrets Required

| Secret Name | Description | Example |
|---|---|---|
| AZUREAPPSERVICE_CLIENTID_F79B82B2BDD44966A925BB5E9CD27472 | Azure Service Principal Client ID | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx |
| AZUREAPPSERVICE_TENANTID_A8D7393D741A4993BBF6CD69147AEB6D | Azure Tenant ID | b925eae5-3023-4f8d-8414-8c56b7cee858 |
| AZUREAPPSERVICE_SUBSCRIPTIONID_CF2560DA9B0A456CA0FE382A922E60B0 | Azure Subscription ID | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx |
| ENTRA_CLIENT_ID | EntraID Application ID | 533594db-31dc-4f88-83e9-b9c0f3a47922 |
| ENTRA_CLIENT_SECRET | EntraID Client Secret | ⚠️ Sensitive - rotate regularly |

## Bicep Parameters

### Required Parameters

- `entraClientSecret` - EntraID application secret (from GitHub Secrets)

### Main Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| environment | string | prod | Environment (prod/dev) |
| location | string | resourceGroup().location | Azure region |
| appName | string | TaxFiler | Application name |
| webAppName | string | - | App Service name |
| keyVaultName | string | - | Key Vault name |
| webAppDomain | string | - | App Service domain |
| entraClientId | string | - | EntraID application ID |
| entraTenantId | string | subscription().tenantId | Azure Tenant ID |
| webAppPrincipalId | string | '' | Web App Managed Identity ID |

## Configuration in App Service

After deployment, App Service has these settings:

```
EntraId:Instance = https://login.microsoftonline.com/
EntraId:Domain = https://taxfiler.azurewebsites.net/ (or dev domain)
EntraId:TenantId = b925eae5-3023-4f8d-8414-8c56b7cee858
EntraId:ClientId = 533594db-31dc-4f88-83e9-b9c0f3a47922
EntraId:ClientSecret = (from Key Vault)
Logging:LogLevel:Default = Information
Logging:LogLevel:Microsoft.AspNetCore = Warning
LlamaParse:AgentId = d8494d42-5bd1-4052-b889-09eade1b740e
```

## Key Vault Structure

```
Key Vault: taxfiler-kv-prod
├─ EntraClientSecret
├─ GoogleDriveClientSecret (manual)
└─ LlamaParseApiKey (manual)
```

To add secrets manually:

```bash
az keyvault secret set \
  --vault-name taxfiler-kv-prod \
  --name SecretName \
  --value "secret-value"
```

## Troubleshooting Quick Fixes

### Deployment fails with permission error
```bash
# Grant Application Administrator role in EntraID
az ad role assignment create \
  --assignee-object-id <SERVICE_PRINCIPAL_ID> \
  --role-definition-id 9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3
```

### App can't access Key Vault
```bash
# Grant Key Vault access to App Service Managed Identity
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query principalId -o tsv)

az keyvault set-policy \
  --name taxfiler-kv-prod \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Check app settings are deployed
```bash
az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query "[?name=='EntraId*']"
```

## GitHub Actions Workflow Steps

1. ✓ Checkout code
2. ✓ Build .NET and Angular
3. ✓ Run tests
4. ✓ Build artifacts
5. ✓ Login to Azure
6. ✓ **Validate Bicep** ← Infrastructure as Code
7. ✓ **Deploy Bicep** ← Infrastructure as Code
8. ✓ Deploy application to App Service

## Documentation Index

- **BICEP_IMPLEMENTATION.md** - Full implementation overview
- **DEPLOYMENT_GUIDE.md** - Detailed deployment instructions
- **GITHUB_SECRETS_SETUP.md** - GitHub Secrets configuration
- **CONFIGURATION_GUIDE.md** - Application configuration management
- **infrastructure/README.md** - Infrastructure templates documentation

## Common Tasks

### Add a new app setting
1. Update `infrastructure/modules/web-app-config.bicep`
2. Add property to `properties` object
3. Push to main (GitHub Actions deploys automatically)

### Add a new secret
1. Add to Azure Key Vault: `az keyvault secret set ...`
2. Reference in app settings: `@Microsoft.KeyVault(SecretUri=...)`
3. Or reference in code from configuration

### Deploy manually (without GitHub Actions)
```bash
az deployment group create \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/prod.bicepparam \
  --parameters entraClientSecret='<YOUR_SECRET>'
```

### Rollback to previous deployment
```bash
# List deployments
az deployment group list --resource-group TaxFiler --query "sort_by([].{name:name, timestamp:properties.timestamp}, &timestamp)[-5:]"

# Redeploy specific version
git checkout <PREVIOUS_COMMIT>
az deployment group create --resource-group TaxFiler ...
```

## Security Checklist

- [ ] GitHub Secrets configured
- [ ] Service Principal has Application Administrator role
- [ ] Key Vault access policies set
- [ ] Managed Identity enabled on App Service
- [ ] Secrets rotated at least quarterly
- [ ] Deployment logs reviewed
- [ ] Application authentication tested

## Support

For detailed information, refer to:
1. **Getting Started** → See DEPLOYMENT_GUIDE.md
2. **GitHub Secrets Issues** → See GITHUB_SECRETS_SETUP.md
3. **App Configuration** → See CONFIGURATION_GUIDE.md
4. **Infrastructure Details** → See infrastructure/README.md

## Related Microsoft Docs

- [Bicep Language](https://aka.ms/bicep)
- [Azure App Service](https://aka.ms/appservice)
- [Azure Key Vault](https://aka.ms/keyvault)
- [GitHub Actions](https://aka.ms/ghactions)
- [Microsoft Entra ID](https://aka.ms/entra)
