# TaxFiler Infrastructure Deployment Guide

This guide provides step-by-step instructions for deploying TaxFiler's infrastructure using Bicep, including EntraID app registration management and Azure App Service configuration.

## Prerequisites

1. **Azure CLI** - Install from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
2. **GitHub CLI** (optional) - For managing secrets: https://cli.github.com/
3. **Azure Subscription** - With appropriate permissions
4. **Azure Credentials** - Service principal with necessary roles

## Quick Start

### 1. Prepare Azure Credentials

```bash
# Login to Azure
az login

# Get current subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

# Create a service principal for CI/CD (if needed)
SERVICE_PRINCIPAL=$(az ad sp create-for-rbac --name "taxfiler-ci" --role "Contributor" --scopes "/subscriptions/$SUBSCRIPTION_ID")

# Extract credentials
CLIENT_ID=$(echo $SERVICE_PRINCIPAL | jq -r '.clientId')
CLIENT_SECRET=$(echo $SERVICE_PRINCIPAL | jq -r '.password')

echo "CLIENT_ID=$CLIENT_ID"
echo "CLIENT_SECRET=$CLIENT_SECRET"
echo "TENANT_ID=$TENANT_ID"
echo "SUBSCRIPTION_ID=$SUBSCRIPTION_ID"
```

### 2. Configure GitHub Secrets

```bash
# Login to GitHub CLI
gh auth login

# Set secrets
gh secret set AZUREAPPSERVICE_CLIENTID_F79B82B2BDD44966A925BB5E9CD27472 \
  --body "$CLIENT_ID" \
  --repo mathiasfritsch/TaxFiler

gh secret set AZUREAPPSERVICE_TENANTID_A8D7393D741A4993BBF6CD69147AEB6D \
  --body "$TENANT_ID" \
  --repo mathiasfritsch/TaxFiler

gh secret set AZUREAPPSERVICE_SUBSCRIPTIONID_CF2560DA9B0A456CA0FE382A922E60B0 \
  --body "$SUBSCRIPTION_ID" \
  --repo mathiasfritsch/TaxFiler

# Set EntraID credentials
gh secret set ENTRA_CLIENT_ID \
  --body "533594db-31dc-4f88-83e9-b9c0f3a47922" \
  --repo mathiasfritsch/TaxFiler

gh secret set ENTRA_CLIENT_SECRET \
  --body "<YOUR_ENTRA_CLIENT_SECRET>" \
  --repo mathiasfritsch/TaxFiler
```

### 3. Validate Bicep Templates

```bash
cd /path/to/TaxFiler

# Lint Bicep templates
az bicep lint infrastructure/main.bicep

# Validate deployment (dry run)
az deployment group validate \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/prod.bicepparam \
  --parameters entraClientId='533594db-31dc-4f88-83e9-b9c0f3a47922' \
  --parameters entraClientSecret='<YOUR_SECRET>'
```

### 4. Deploy Infrastructure

```bash
# Create resource group (if not exists)
az group create \
  --name TaxFiler \
  --location EastUS

# Deploy infrastructure
az deployment group create \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/prod.bicepparam \
  --parameters entraClientId='533594db-31dc-4f88-83e9-b9c0f3a47922' \
  --parameters entraClientSecret='<YOUR_SECRET>' \
  --parameters webAppName='TaxFiler' \
  --parameters webAppDomain='taxfiler.azurewebsites.net' \
  --name taxfiler-infra-prod-$(date +%s)
```

### 5. Verify Deployment

```bash
# Get deployment outputs
az deployment group show \
  --resource-group TaxFiler \
  --name <DEPLOYMENT_NAME> \
  --query properties.outputs

# Check Key Vault was created
az keyvault list --resource-group TaxFiler

# Check App Service settings
az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler
```

## Manual Configuration (Alternative)

If you prefer to deploy without GitHub Actions, follow these steps:

### Step 1: Create Resource Group

```bash
az group create \
  --name TaxFiler \
  --location EastUS
```

### Step 2: Prepare Parameters

Create a file `deploy-params.txt`:

```
environment=prod
location=EastUS
appName=TaxFiler
webAppName=TaxFiler
resourceGroupName=TaxFiler
keyVaultName=taxfiler-kv-prod
webAppDomain=taxfiler.azurewebsites.net
entraClientId=533594db-31dc-4f88-83e9-b9c0f3a47922
entraClientSecret=<YOUR_SECRET>
entraTenantId=b925eae5-3023-4f8d-8414-8c56b7cee858
webAppPrincipalId=<WEB_APP_MANAGED_IDENTITY_ID>
```

### Step 3: Deploy

```bash
az deployment group create \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/prod.bicepparam \
  --parameters @deploy-params.txt
```

## Deployment Troubleshooting

### Issue: "Insufficient privileges to complete the operation"

**Solution:**
- Ensure service principal has Application Administrator role in EntraID
- Grant Directory.ReadWrite.All permissions to the service principal

```bash
# Grant Application Administrator role
az ad role assignment create \
  --assignee-object-id <SERVICE_PRINCIPAL_ID> \
  --assignee-principal-type ServicePrincipal \
  --role-definition-id 9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3 \
  --scope /
```

### Issue: "Resource already exists"

**Solution:**
- Bicep will update existing resources
- Check if resource has conflicting properties
- Delete and redeploy if necessary

```bash
# List existing resources
az resource list --resource-group TaxFiler --query "[].name"

# Delete if needed
az resource delete \
  --resource-group TaxFiler \
  --name <RESOURCE_NAME> \
  --resource-type <RESOURCE_TYPE>
```

### Issue: "Key Vault access denied"

**Solution:**
- Ensure App Service managed identity has access policy
- Add access policy manually if needed

```bash
# Get App Service managed identity
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query principalId -o tsv)

# Add Key Vault access policy
az keyvault set-policy \
  --name taxfiler-kv-prod \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

## Post-Deployment Steps

### 1. Verify Application Configuration

```bash
# Check EntraID settings
az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query "[?name=='EntraId*']"
```

### 2. Test Authentication

1. Navigate to https://taxfiler.azurewebsites.net
2. Click login
3. Verify EntraID login works
4. Check Swagger UI at https://taxfiler.azurewebsites.net/swagger

### 3. Store Secrets in Key Vault

```bash
# Add sensitive values to Key Vault
az keyvault secret set \
  --vault-name taxfiler-kv-prod \
  --name EntraClientSecret \
  --value "<YOUR_SECRET>"

az keyvault secret set \
  --vault-name taxfiler-kv-prod \
  --name GoogleDriveClientSecret \
  --value "<YOUR_SECRET>"

az keyvault secret set \
  --vault-name taxfiler-kv-prod \
  --name LlamaParseApiKey \
  --value "<YOUR_API_KEY>"
```

### 4. Update App Service to Use Key Vault

Update App Service configuration to reference Key Vault secrets:

```bash
az webapp config appsettings set \
  --resource-group TaxFiler \
  --name TaxFiler \
  --settings \
    "EntraId:ClientSecret=@Microsoft.KeyVault(SecretUri=https://taxfiler-kv-prod.vault.azure.net/secrets/EntraClientSecret/)" \
    "GoogleDriveSettings:ClientSecret=@Microsoft.KeyVault(SecretUri=https://taxfiler-kv-prod.vault.azure.net/secrets/GoogleDriveClientSecret/)" \
    "LlamaParse:ApiKey=@Microsoft.KeyVault(SecretUri=https://taxfiler-kv-prod.vault.azure.net/secrets/LlamaParseApiKey/)"
```

## Updating Infrastructure

To update the infrastructure:

1. **Modify Bicep templates** in `infrastructure/`
2. **Validate changes** locally:
   ```bash
   az bicep lint infrastructure/main.bicep
   az deployment group validate --resource-group TaxFiler --template-file infrastructure/main.bicep
   ```
3. **Deploy updated infrastructure**:
   ```bash
   az deployment group create \
     --resource-group TaxFiler \
     --template-file infrastructure/main.bicep \
     --parameters infrastructure/parameters/prod.bicepparam \
     --parameters entraClientSecret='<YOUR_SECRET>'
   ```
4. **Commit changes** to Git for audit trail

## Monitoring Deployments

### View Deployment History

```bash
# List recent deployments
az deployment group list \
  --resource-group TaxFiler \
  --query "sort_by([].{name:name, timestamp:properties.timestamp, state:properties.provisioningState}, &timestamp)" \
  -o table

# Get details of specific deployment
az deployment group show \
  --resource-group TaxFiler \
  --name <DEPLOYMENT_NAME>
```

### Monitor with Azure Portal

1. Go to https://portal.azure.com
2. Navigate to Resource Groups > TaxFiler
3. Click "Deployments" to see history
4. View deployment details and outputs

## Rollback Strategy

If deployment fails or needs to be rolled back:

1. **Identify last successful deployment:**
   ```bash
   az deployment group list \
     --resource-group TaxFiler \
     --query "max_by([?properties.provisioningState=='Succeeded'], &properties.timestamp)"
   ```

2. **Redeploy previous version:**
   ```bash
   git checkout <PREVIOUS_COMMIT>
   az deployment group create \
     --resource-group TaxFiler \
     --template-file infrastructure/main.bicep \
     --parameters infrastructure/parameters/prod.bicepparam
   ```

3. **Verify rollback success:**
   ```bash
   az webapp restart --resource-group TaxFiler --name TaxFiler
   ```

## Best Practices

1. **Always validate before deploying**
   ```bash
   az deployment group validate --resource-group TaxFiler --template-file infrastructure/main.bicep
   ```

2. **Use parameter files for different environments**
   - dev.bicepparam for development
   - prod.bicepparam for production

3. **Version infrastructure** in Git
   - Track all infrastructure changes
   - Review infrastructure PRs before merging

4. **Document changes** with clear commit messages

5. **Test in dev first** before deploying to production

6. **Keep secrets secure**
   - Never commit secrets to Git
   - Use GitHub Secrets and Key Vault
   - Rotate secrets regularly

## References

- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure CLI Documentation](https://docs.microsoft.com/en-us/cli/azure/)
- [App Service Configuration](https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings)
- [Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)

## Support

For issues or questions:

1. Check the troubleshooting section
2. Review deployment logs: `az deployment group list --resource-group TaxFiler`
3. Check Azure Portal for resource health
4. Consult Azure documentation linked above
