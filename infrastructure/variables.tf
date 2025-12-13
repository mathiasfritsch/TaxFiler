variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
  default     = "25010af9-7f24-4aca-9123-f7054c0566f7"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "TaxFiler_group"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "germanywestcentral"
}

variable "web_app_name" {
  description = "Name of the web app"
  type        = string
  default     = "TaxFilerTF"
}

variable "app_service_plan_name" {
  description = "Name of the App Service Plan"
  type        = string
  default     = "ASP-TaxFilergroup-a6ab"
}

variable "app_service_plan_sku" {
  description = "SKU for the App Service Plan"
  type        = string
  default     = "B1"
  
  validation {
    condition     = contains(["F1", "B1", "B2", "B3", "S1", "S2", "S3", "P1v2", "P2v2", "P3v2"], var.app_service_plan_sku)
    error_message = "The app_service_plan_sku must be one of: F1, B1, B2, B3, S1, S2, S3, P1v2, P2v2, P3v2."
  }
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}

