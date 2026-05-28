// Bicep module for managing EntraID application registration
// This module creates and manages the EntraID app registration for TaxFiler

metadata name = 'EntraID Application Registration'
metadata description = 'Creates and manages EntraID app registration for TaxFiler application'
metadata owner = 'TaxFiler Team'

param appName string = 'TaxFiler'
param appDescription string = 'Tax document processing application with AI-powered extraction'
param redirectUris array = []
param logoutUrl string = ''
param requiredResourceAccess array = []
param environment string = 'prod'

// Reference to an existing EntraID app - this will be looked up instead of created
// In a real scenario, you might use a data source or reference an existing app
resource entraAppRegistration 'Microsoft.Graph/applications@v1.0' = {
  displayName: appName
  description: appDescription
  signInAudience: 'AzureADMyOrg'
  
  web: {
    redirectUris: redirectUris
    implicitGrantSettings: {
      enableAccessTokenIssuance: false
      enableIdTokenIssuance: true
    }
    logoutUrl: logoutUrl
  }

  api: {
    oauth2PermissionScopes: [
      {
        adminConsentDescription: 'Access TaxFiler API'
        adminConsentDisplayName: 'Access TaxFiler API'
        id: guid(resourceGroup().id, 'default_access')
        isEnabled: true
        type: 'User'
        userConsentDescription: 'Access TaxFiler API on your behalf'
        userConsentDisplayName: 'Access TaxFiler API'
        value: 'default_access'
      }
    ]
  }

  requiredResourceAccess: requiredResourceAccess
  
  tags: [
    'app:taxfiler'
    'env:${environment}'
  ]
}

// Create service principal for the application
resource servicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: entraAppRegistration.appId
  displayName: appName
  servicePrincipalType: 'Application'
  tags: [
    'app:taxfiler'
    'env:${environment}'
  ]
}

// Output the important values for later use
output applicationId string = entraAppRegistration.appId
output tenantId string = subscription().tenantId
output objectId string = entraAppRegistration.id
output servicePrincipalId string = servicePrincipal.id
output defaultScopeId string = guid(resourceGroup().id, 'default_access')
