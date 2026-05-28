# GitHub Secrets Configuration for TaxFiler

This document describes the GitHub Secrets required for the TaxFiler CI/CD pipeline with Bicep infrastructure deployment.

## Required GitHub Secrets

To enable the automated deployment of TaxFiler infrastructure and application, you need to configure the following GitHub Secrets in your repository settings (Settings > Secrets and variables > Actions):

### Azure Service Principal Credentials

These credentials allow GitHub Actions to authenticate and deploy resources to Azure:

1. **AZUREAPPSERVICE_CLIENTID_F79B82B2BDD44966A925BB5E9CD27472**
   - Description: Client ID of the Azure service principal
   - Value: `<YOUR_SERVICE_PRINCIPAL_CLIENT_ID>`
   - Obtain from: `az ad sp list --display-name <service-principal-name> --query "[0].appId"`

2. **AZUREAPPSERVICE_TENANTID_A8D7393D741A4993BBF6CD69147AEB6D**
   - Description: Tenant ID of your Azure subscription
   - Value: `b925eae5-3023-4f8d-8414-8c56b7cee858`
   - Obtain from: `az account show --query tenantId`

3. **AZUREAPPSERVICE_SUBSCRIPTIONID_CF2560DA9B0A456CA0FE382A922E60B0**
   - Description: Azure subscription ID
   - Value: `<YOUR_SUBSCRIPTION_ID>`
   - Obtain from: `az account show --query id`

### EntraID Application Credentials

These credentials are used to configure the EntraID app registration via Bicep:

4. **ENTRA_CLIENT_ID**
   - Description: Client ID (Application ID) of the EntraID application
   - Value: `533594db-31dc-4f88-83e9-b9c0f3a47922`
   - Obtain from: Azure Portal > EntraID > App registrations > TaxFiler

5. **ENTRA_CLIENT_SECRET**
   - Description: Client secret of the EntraID application
   - Value: `<YOUR_ENTRA_CLIENT_SECRET>`
   - ⚠️ **IMPORTANT**: This is sensitive and should be rotated regularly
   - Obtain from: Azure Portal > EntraID > App registrations > TaxFiler > Certificates & secrets

## Setting Up GitHub Secrets

### Via GitHub Web UI

1. Go to your repository on GitHub
2. Click Settings (top right)
3. Navigate to "Secrets and variables" > "Actions"
4. Click "New repository secret"
5. Enter the secret name and value
6. Click "Add secret"

### Via GitHub CLI

```bash
# Ensure you're logged in to GitHub CLI
gh auth login

# Add a secret
gh secret set SECRET_NAME --body "secret_value" --repo mathiasfritsch/TaxFiler
```

### Recommended Setup Script

```bash
#!/bin/bash

# Configure Azure credentials
gh secret set AZUREAPPSERVICE_CLIENTID_F79B82B2BDD44966A925BB5E9CD27472 \
  --body "$(az ad sp list --display-name <service-principal-name> --query '[0].appId' -o tsv)" \
  --repo mathiasfritsch/TaxFiler

gh secret set AZUREAPPSERVICE_TENANTID_A8D7393D741A4993BBF6CD69147AEB6D \
  --body "$(az account show --query tenantId -o tsv)" \
  --repo mathiasfritsch/TaxFiler

gh secret set AZUREAPPSERVICE_SUBSCRIPTIONID_CF2560DA9B0A456CA0FE382A922E60B0 \
  --body "$(az account show --query id -o tsv)" \
  --repo mathiasfritsch/TaxFiler

# Configure EntraID credentials
gh secret set ENTRA_CLIENT_ID \
  --body "533594db-31dc-4f88-83e9-b9c0f3a47922" \
  --repo mathiasfritsch/TaxFiler

gh secret set ENTRA_CLIENT_SECRET \
  --body "<YOUR_ENTRA_CLIENT_SECRET>" \
  --repo mathiasfritsch/TaxFiler
```

## Secret Rotation

### EntraID Client Secret Rotation

1. Go to Azure Portal > EntraID > App registrations > TaxFiler
2. Navigate to "Certificates & secrets"
3. Add a new client secret under "Client secrets"
4. Copy the new secret value
5. Update the `ENTRA_CLIENT_SECRET` GitHub Secret
6. Delete the old client secret from Azure Portal after confirming the new one works
7. Test the deployment workflow to ensure it succeeds

### Azure Service Principal Credential Rotation

1. List service principals: `az ad sp list --display-name <name>`
2. Create a new service principal credential
3. Update the GitHub Secrets with new values
4. Test the deployment
5. Delete the old credentials

## Security Best Practices

1. **Minimize Secret Exposure**
   - Never commit secrets to the repository
   - Use GitHub Secrets for all sensitive values
   - Rotate secrets regularly (at least annually)

2. **Principle of Least Privilege**
   - Use a dedicated service principal with only necessary permissions
   - Grant the service principal only the required roles:
     - Contributor on the resource group (or more specific roles)
     - Application Administrator in EntraID (for app registration management)

3. **Audit Access**
   - Monitor who has access to GitHub repository settings
   - Review GitHub Actions logs for secret usage
   - Enable branch protection rules for main branch

4. **Secret Scope**
   - Keep secrets scoped to the repository
   - Use environment secrets for environment-specific values (if using multiple environments)

## Troubleshooting

### Deployment Fails with "Authentication Failed"

1. Verify Azure credentials are correct:
   ```bash
   az login --service-principal -u <CLIENT_ID> -p <CLIENT_SECRET> --tenant <TENANT_ID>
   ```

2. Check service principal has correct permissions:
   ```bash
   az role assignment list --assignee <SERVICE_PRINCIPAL_ID>
   ```

### Deployment Fails with "App Registration Not Found"

1. Verify the EntraID app exists in your tenant:
   ```bash
   az ad app list --filter "displayName eq 'TaxFiler'" --query "[0].appId"
   ```

2. Ensure the service principal has Application Administrator role in EntraID

### Bicep Validation Errors

1. Run local validation:
   ```bash
   az bicep lint infrastructure/main.bicep
   ```

2. Check parameter files are correctly formatted:
   ```bash
   cat infrastructure/parameters/prod.bicepparam
   ```

## References

- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
- [Azure CLI Authentication](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli)
- [Azure Service Principals](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals)
- [EntraID App Management](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
