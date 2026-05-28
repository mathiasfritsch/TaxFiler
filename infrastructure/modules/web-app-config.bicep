// Bicep module for configuring Web App with EntraID settings
// This module configures the Azure App Service with application settings

metadata name = 'Web App Configuration'
metadata description = 'Configures Azure App Service with EntraID and other application settings'

param webAppName string
param resourceGroupName string
param entraClientId string
@secure()
param entraClientSecret string
param entraInstance string = 'https://login.microsoftonline.com/'
param entraTenantId string
param webAppDomain string
param keyVaultUri string = ''

// Reference the existing Web App
resource webApp 'Microsoft.Web/sites@2023-01-01' existing = {
  name: webAppName
  resourceGroup: resourceGroupName
}

// Configure application settings
resource appSettings 'Microsoft.Web/sites/config@2023-01-01' = {
  parent: webApp
  name: 'appsettings'
  properties: {
    // EntraID Configuration
    'EntraId:Instance': entraInstance
    'EntraId:Domain': webAppDomain
    'EntraId:TenantId': entraTenantId
    'EntraId:ClientId': entraClientId
    'EntraId:ClientSecret': entraClientSecret
    
    // Application Configuration
    'Logging:LogLevel:Default': 'Information'
    'Logging:LogLevel:Microsoft.AspNetCore': 'Warning'
    
    // Connection Strings via app settings (can be overridden by Key Vault)
    'ConnectionStrings:TaxFilerNeonDB': ''
    
    // LlamaParse Configuration (API key should come from Key Vault or user secrets)
    'LlamaParse:AgentId': 'd8494d42-5bd1-4052-b889-09eade1b740e'
  }
}

// Authentication settings
resource authSettings 'Microsoft.Web/sites/config@2023-01-01' = {
  parent: webApp
  name: 'authsettingsV2'
  properties: {
    globalValidation: {
      requireAuthentication: false
      unauthenticatedClientAction: 'AllowAnonymous'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          openIdIssuer: '${entraInstance}${entraTenantId}/v2.0'
          clientId: entraClientId
          clientSecretSettingName: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
        }
        validation: {
          jwtClaimChecks: {
            allowedAppIds: []
          }
          allowedAudiences: [
            entraClientId
          ]
        }
      }
    }
    login: {
      tokenStore: {
        enabled: true
      }
    }
  }
}

output appSettingsResourceId string = appSettings.id
output authSettingsResourceId string = authSettings.id
