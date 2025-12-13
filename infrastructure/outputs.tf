output "web_app_name" {
  description = "Name of the web app"
  value       = azurerm_linux_web_app.taxfiler.name
}

output "web_app_url" {
  description = "URL of the web app"
  value       = "https://${azurerm_linux_web_app.taxfiler.default_hostname}"
}

output "web_app_id" {
  description = "ID of the web app"
  value       = azurerm_linux_web_app.taxfiler.id
}

output "app_service_plan_id" {
  description = "ID of the App Service Plan"
  value       = data.azurerm_service_plan.taxfiler.id
}

output "app_service_plan_sku" {
  description = "SKU of the existing App Service Plan"
  value       = data.azurerm_service_plan.taxfiler.sku_name
}

output "outbound_ip_addresses" {
  description = "Outbound IP addresses of the web app"
  value       = azurerm_linux_web_app.taxfiler.outbound_ip_addresses
}
