# TaxFiler Bicep Implementation - Getting Started Checklist

This checklist guides you through setting up and verifying the Bicep Infrastructure as Code implementation.

## Phase 1: Prerequisites (15 minutes)

- [ ] Azure CLI installed: `az --version`
- [ ] GitHub CLI installed (optional): `gh --version`
- [ ] Logged into Azure: `az login`
- [ ] Logged into GitHub CLI (if using): `gh auth login`
- [ ] Have access to Azure subscription
- [ ] Have Application Administrator role in EntraID (for app registration)
- [ ] Have GitHub repository admin access (to set secrets)

### Verification Commands

```bash
# Verify Azure CLI
az --version

# Verify logged into Azure
az account show

# Verify subscription
az account list -o table
```

## Phase 2: Azure Infrastructure Setup (30 minutes)

### Step 1: Create Resource Group

```bash
# Create resource group
az group create --name TaxFiler --location EastUS

# Verify
az group show --name TaxFiler
```

- [ ] Resource group created

### Step 2: Create App Service Plan and App

```bash
# Create App Service Plan
az appservice plan create \
  --name TaxFilerPlan \
  --resource-group TaxFiler \
  --sku F1

# Create Web App
az webapp create \
  --resource-group TaxFiler \
  --plan TaxFilerPlan \
  --name TaxFiler

# Enable managed identity (required for Key Vault)
az webapp identity assign \
  --resource-group TaxFiler \
  --name TaxFiler
```

- [ ] App Service Plan created
- [ ] Web App created
- [ ] Managed Identity enabled

### Step 3: Create Azure Service Principal for CI/CD

```bash
# Create service principal
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SERVICE_PRINCIPAL=$(az ad sp create-for-rbac \
  --name taxfiler-ci \
  --role Contributor \
  --scopes "/subscriptions/$SUBSCRIPTION_ID")

# Extract values
CLIENT_ID=$(echo $SERVICE_PRINCIPAL | jq -r '.clientId')
CLIENT_SECRET=$(echo $SERVICE_PRINCIPAL | jq -r '.password')
TENANT_ID=$(echo $SERVICE_PRINCIPAL | jq -r '.tenant')

# Grant Application Administrator role for EntraID app management
PRINCIPAL_OBJECT_ID=$(az ad sp show \
  --id $CLIENT_ID \
  --query objectId -o tsv)

az ad role assignment create \
  --assignee-object-id $PRINCIPAL_OBJECT_ID \
  --role-definition-id 9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3 \
  --scope "/"

echo "CLIENT_ID=$CLIENT_ID"
echo "CLIENT_SECRET=$CLIENT_SECRET"
echo "TENANT_ID=$TENANT_ID"
echo "SUBSCRIPTION_ID=$SUBSCRIPTION_ID"
```

- [ ] Service Principal created
- [ ] Application Administrator role assigned
- [ ] Credentials saved securely

## Phase 3: GitHub Secrets Configuration (20 minutes)

### Step 1: Set Azure Credentials Secrets

```bash
# Using GitHub CLI
CLIENT_ID="your-client-id"
CLIENT_SECRET="your-client-secret"
TENANT_ID="your-tenant-id"
SUBSCRIPTION_ID="your-subscription-id"

gh secret set AZUREAPPSERVICE_CLIENTID_F79B82B2BDD44966A925BB5E9CD27472 \
  --body "$CLIENT_ID" \
  --repo mathiasfritsch/TaxFiler

gh secret set AZUREAPPSERVICE_TENANTID_A8D7393D741A4993BBF6CD69147AEB6D \
  --body "$TENANT_ID" \
  --repo mathiasfritsch/TaxFiler

gh secret set AZUREAPPSERVICE_SUBSCRIPTIONID_CF2560DA9B0A456CA0FE382A922E60B0 \
  --body "$SUBSCRIPTION_ID" \
  --repo mathiasfritsch/TaxFiler
```

- [ ] AZUREAPPSERVICE_CLIENTID_F79B82B2BDD44966A925BB5E9CD27472 set
- [ ] AZUREAPPSERVICE_TENANTID_A8D7393D741A4993BBF6CD69147AEB6D set
- [ ] AZUREAPPSERVICE_SUBSCRIPTIONID_CF2560DA9B0A456CA0FE382A922E60B0 set

### Step 2: Set EntraID Secrets

```bash
ENTRA_CLIENT_ID="533594db-31dc-4f88-83e9-b9c0f3a47922"
ENTRA_CLIENT_SECRET="your-entra-client-secret"

gh secret set ENTRA_CLIENT_ID \
  --body "$ENTRA_CLIENT_ID" \
  --repo mathiasfritsch/TaxFiler

gh secret set ENTRA_CLIENT_SECRET \
  --body "$ENTRA_CLIENT_SECRET" \
  --repo mathiasfritsch/TaxFiler
```

- [ ] ENTRA_CLIENT_ID set
- [ ] ENTRA_CLIENT_SECRET set

### Step 3: Verify Secrets (in GitHub UI)

1. Go to https://github.com/mathiasfritsch/TaxFiler
2. Settings > Secrets and variables > Actions
3. Verify all 5 secrets are present

- [ ] All 5 GitHub Secrets configured

## Phase 4: Test Infrastructure Deployment (30 minutes)

### Step 1: Validate Bicep Locally

```bash
cd /path/to/TaxFiler

# Lint templates
az bicep lint infrastructure/main.bicep

# Validate deployment (dry run)
az deployment group validate \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/prod.bicepparam \
  --parameters entraClientSecret="$ENTRA_CLIENT_SECRET"
```

- [ ] Bicep linting successful
- [ ] Deployment validation successful

### Step 2: Deploy Locally (Optional)

```bash
# Deploy infrastructure
az deployment group create \
  --resource-group TaxFiler \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters/prod.bicepparam \
  --parameters entraClientSecret="$ENTRA_CLIENT_SECRET" \
  --parameters webAppName='TaxFiler' \
  --parameters webAppDomain='taxfiler.azurewebsites.net' \
  --name taxfiler-infra-prod-test

# Check deployment status
az deployment group show \
  --resource-group TaxFiler \
  --name taxfiler-infra-prod-test \
  --query properties.provisioningState
```

- [ ] Infrastructure deployed successfully
- [ ] Deployment status shows "Succeeded"

### Step 3: Verify Deployment

```bash
# Check Key Vault created
az keyvault list --resource-group TaxFiler

# Check App Service settings
az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query "[?name=='EntraId*']"

# Check if managed identity has Key Vault access
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query principalId -o tsv)

az keyvault show \
  --resource-group TaxFiler \
  --name taxfiler-kv-prod \
  --query properties.accessPolicies[?objectId=='$PRINCIPAL_ID']
```

- [ ] Key Vault created
- [ ] EntraID settings in App Service
- [ ] Managed Identity has Key Vault access

## Phase 5: GitHub Actions Workflow Test (30 minutes)

### Step 1: Trigger Workflow

1. Make a small change to the repository (e.g., add a comment to infrastructure/README.md)
2. Commit and push to main branch
3. Monitor workflow at https://github.com/mathiasfritsch/TaxFiler/actions

```bash
git add infrastructure/README.md
git commit -m "Test workflow trigger"
git push origin main
```

- [ ] Pushed test change to main

### Step 2: Monitor Workflow

1. Go to GitHub Actions
2. Watch the workflow run
3. Check for:
   - Build success
   - Test success
   - Bicep validation
   - Infrastructure deployment
   - Application deployment

- [ ] Workflow completes successfully
- [ ] All steps pass

### Step 3: Verify Post-Deployment

```bash
# Check latest deployment
az deployment group list \
  --resource-group TaxFiler \
  --query "sort_by([].{name:name, timestamp:properties.timestamp}, &timestamp)[-1]"

# Verify app settings still present
az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query "[].name" | grep -i entra
```

- [ ] Latest deployment shows "Succeeded"
- [ ] App settings verified

## Phase 6: Post-Deployment Configuration (30 minutes)

### Step 1: Add Secrets to Key Vault

```bash
# Get Key Vault name
KV_NAME=$(az keyvault list \
  --resource-group TaxFiler \
  --query "[0].name" -o tsv)

# Add Google Drive client secret
az keyvault secret set \
  --vault-name $KV_NAME \
  --name GoogleDriveClientSecret \
  --value "your-google-drive-secret"

# Add LlamaParse API key
az keyvault secret set \
  --vault-name $KV_NAME \
  --name LlamaParseApiKey \
  --value "your-llamaparse-api-key"
```

- [ ] Google Drive client secret added
- [ ] LlamaParse API key added

### Step 2: Configure App to Use Key Vault Secrets

```bash
# Update app settings to reference Key Vault
KV_URI=$(az keyvault show \
  --resource-group TaxFiler \
  --name $KV_NAME \
  --query properties.vaultUri -o tsv)

az webapp config appsettings set \
  --resource-group TaxFiler \
  --name TaxFiler \
  --settings \
    "GoogleDriveSettings:ClientSecret=@Microsoft.KeyVault(SecretUri=${KV_URI}secrets/GoogleDriveClientSecret/)" \
    "LlamaParse:ApiKey=@Microsoft.KeyVault(SecretUri=${KV_URI}secrets/LlamaParseApiKey/)"
```

- [ ] App configured to use Key Vault secrets

### Step 3: Restart Application

```bash
az webapp restart --resource-group TaxFiler --name TaxFiler
```

- [ ] Application restarted

## Phase 7: Verification (30 minutes)

### Step 1: Test Authentication

1. Navigate to https://taxfiler.azurewebsites.net
2. Click "Login" or "Sign in"
3. Verify EntraID login appears
4. Attempt to login with test account
5. Verify successful authentication

- [ ] Login redirects to EntraID
- [ ] Authentication successful
- [ ] User logged in

### Step 2: Check Application Logs

```bash
# Stream application logs
az webapp log tail \
  --resource-group TaxFiler \
  --name TaxFiler \
  --provider applicationinsights

# Or check deployment center logs in Azure Portal
```

- [ ] No authentication errors in logs
- [ ] Application running normally

### Step 3: Verify Key Vault Access

```bash
# Check that app can access secrets
# This can be verified in Application Insights or logs

# Manual verification - check that settings containing @Microsoft.KeyVault
# are being resolved

az webapp config appsettings list \
  --resource-group TaxFiler \
  --name TaxFiler \
  --query "[?value contains '@Microsoft.KeyVault']"
```

- [ ] Key Vault secrets being resolved
- [ ] No access errors

## Phase 8: Documentation Review (15 minutes)

- [ ] Read docs/BICEP_IMPLEMENTATION.md
- [ ] Read docs/DEPLOYMENT_GUIDE.md (skim)
- [ ] Read docs/QUICK_REFERENCE.md
- [ ] Bookmark docs for future reference
- [ ] Share documentation with team

## Troubleshooting

If you encounter issues, refer to:

1. **Deployment fails** → See docs/DEPLOYMENT_GUIDE.md "Troubleshooting"
2. **GitHub Secrets issues** → See docs/GITHUB_SECRETS_SETUP.md
3. **Application config issues** → See docs/CONFIGURATION_GUIDE.md
4. **Infrastructure questions** → See infrastructure/README.md
5. **Quick lookup** → See docs/QUICK_REFERENCE.md

## Success Criteria

You've successfully implemented Bicep Infrastructure as Code when:

- [x] All GitHub Secrets configured
- [x] Resource Group and App Service created
- [x] Service Principal created with proper permissions
- [x] Bicep templates validated locally
- [x] Infrastructure deployed successfully
- [x] GitHub Actions workflow completes successfully
- [x] Application authentication works
- [x] Key Vault configured
- [x] Documentation reviewed
- [x] Team notified of completion

## Next Steps

1. **Ongoing Operations**
   - Monitor deployments in GitHub Actions
   - Keep secrets rotated
   - Review infrastructure changes
   - Update documentation as needed

2. **Infrastructure Improvements**
   - Add Application Insights
   - Configure alerts and monitoring
   - Implement cost management
   - Add automated backups

3. **Security Hardening**
   - Implement network security groups
   - Configure private endpoints
   - Enable WAF on App Service
   - Regular security audits

## Support Resources

- **Bicep Documentation**: https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/
- **GitHub Actions Documentation**: https://docs.github.com/en/actions
- **Azure App Service Documentation**: https://learn.microsoft.com/en-us/azure/app-service/
- **Azure Key Vault Documentation**: https://learn.microsoft.com/en-us/azure/key-vault/
- **Microsoft Entra ID Documentation**: https://learn.microsoft.com/en-us/entra/

## Questions?

For implementation questions:
1. Check the documentation in `/docs` directory
2. Review infrastructure templates in `/infrastructure` directory
3. Consult Azure and GitHub documentation
4. Ask team members or maintainers
