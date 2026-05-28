// Main Bicep template for TaxFiler Infrastructure as Code
// Orchestrates the deployment of all infrastructure components

metadata name = 'TaxFiler Infrastructure'
metadata description = 'Main infrastructure template for TaxFiler application with EntraID app registration'

targetScope = 'resourceGroup'

// Parameters
param environment string = 'prod'
param location string = resourceGroup().location
param appName string = 'TaxFiler'
param webAppName string
param resourceGroupName string = resourceGroup().name
param keyVaultName string
param webAppDomain string
@secure()
param entraClientSecret string
param entraClientId string = ''
param entraTenantId string = subscription().tenantId
param webAppPrincipalId string = ''

// Variables
var tags = {
  environment: environment
  application: 'taxfiler'
  managedBy: 'bicep'
  createdOn: utcNow('u')
}

var redirectUris = [
  'https://${webAppDomain}/signin-oidc'
  'https://${webAppDomain}/swagger/oauth2-redirect.html'
]

// Module: Deploy Key Vault
module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault-deployment'
  params: {
    keyVaultName: keyVaultName
    location: location
    environment: environment
    webAppPrincipalId: webAppPrincipalId
    tags: tags
  }
}

// Module: Configure Web App settings
module webAppConfig 'modules/web-app-config.bicep' = {
  name: 'webApp-config-deployment'
  params: {
    webAppName: webAppName
    resourceGroupName: resourceGroupName
    entraClientId: entraClientId
    entraClientSecret: entraClientSecret
    entraTenantId: entraTenantId
    webAppDomain: webAppDomain
    keyVaultUri: keyVault.outputs.keyVaultUri
  }
}

// Outputs
output deploymentResourceId string = resourceGroup().id
output keyVaultUri string = keyVault.outputs.keyVaultUri
output keyVaultId string = keyVault.outputs.keyVaultId
output webAppConfigResourceId string = webAppConfig.outputs.appSettingsResourceId
output environment string = environment
output location string = location
output tags object = tags
