terraform {
  required_version = ">= 1.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Optional: Terraform State Backend in Azure Storage
  # backend "azurerm" {
  #   resource_group_name  = "TaxFiler_group"
  #   storage_account_name = "taxfilerterraformstate"
  #   container_name       = "tfstate"
  #   key                  = "terraform.tfstate"
  # }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

# Data sources for existing resources
data "azurerm_user_assigned_identity" "taxfiler" {
  name                = "TaxFilerIdentity"
  resource_group_name = "TaxFilerDbResourceGroup"
}

data "azurerm_application_insights" "taxfiler" {
  name                = "TaxFiler"
  resource_group_name = var.resource_group_name
}

# Use existing App Service Plan
data "azurerm_service_plan" "taxfiler" {
  name                = var.app_service_plan_name
  resource_group_name = var.resource_group_name
}

# Linux Web App
resource "azurerm_linux_web_app" "taxfiler" {
  name                = var.web_app_name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = data.azurerm_service_plan.taxfiler.id

  https_only                    = true
  public_network_access_enabled = true
  client_affinity_enabled       = false
  
  identity {
    type = "SystemAssigned, UserAssigned"
    identity_ids = [
      data.azurerm_user_assigned_identity.taxfiler.id
    ]
  }

  site_config {
    always_on        = can(regex("^(F1|D1)$", data.azurerm_service_plan.taxfiler.sku_name)) ? false : true
    http2_enabled    = false
    ftps_state       = "FtpsOnly"
    
    application_stack {
      dotnet_version = "10.0"
    }

    app_command_line = "dotnet TaxFiler.Server.dll"

    ip_restriction {
      action     = "Allow"
      name       = "Allow all"
      priority   = 2147483647
      ip_address = "0.0.0.0/0"
    }

    scm_use_main_ip_restriction = false
    
    scm_ip_restriction {
      action     = "Allow"
      name       = "Allow all"
      priority   = 2147483647
      ip_address = "0.0.0.0/0"
    }

    minimum_tls_version           = "1.2"
    scm_minimum_tls_version       = "1.2"
    use_32_bit_worker             = true
    websockets_enabled            = false
    vnet_route_all_enabled        = false
    local_mysql_enabled           = false
  }

  app_settings = {
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = data.azurerm_application_insights.taxfiler.connection_string
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    "XDT_MicrosoftApplicationInsights_Mode" = "recommended"
  }

  tags = merge(
    var.tags,
    {
      "hidden-link: /app-insights-resource-id" = data.azurerm_application_insights.taxfiler.id
    }
  )

  lifecycle {
    ignore_changes = [
      # Ignore changes to app_settings that might be set via portal or deployment
      app_settings["WEBSITE_ENABLE_SYNC_UPDATE_SITE"],
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
    ]
  }
}
