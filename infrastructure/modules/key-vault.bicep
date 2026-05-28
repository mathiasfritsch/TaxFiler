// Bicep module for managing Azure Key Vault
// This module creates and configures Key Vault for storing sensitive configuration

metadata name = 'Azure Key Vault'
metadata description = 'Creates and configures Azure Key Vault for TaxFiler secrets'

param keyVaultName string
param location string = resourceGroup().location
param environment string = 'prod'
param enabledForDeployment bool = true
param enabledForTemplateDeployment bool = true
param enabledForDiskEncryption bool = false
param webAppPrincipalId string = ''

param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    enabledForDeployment: enabledForDeployment
    enabledForTemplateDeployment: enabledForTemplateDeployment
    enabledForDiskEncryption: enabledForDiskEncryption
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
    publicNetworkAccess: 'Enabled'
  }
  tags: union(tags, {
    environment: environment
    app: 'taxfiler'
  })
}

// Grant Web App access to Key Vault (if principal ID provided)
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = if (!empty(webAppPrincipalId)) {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: webAppPrincipalId
        permissions: {
          keys: [
            'get'
            'list'
          ]
          secrets: [
            'get'
            'list'
          ]
          certificates: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultName string = keyVault.name
