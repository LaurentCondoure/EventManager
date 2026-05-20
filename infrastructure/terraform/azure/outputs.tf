output "app_service_url" {
  value       = "https://${azurerm_linux_web_app.api.default_hostname}"
  description = "URL publique de l'API"
}

output "sql_server_fqdn" {
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
  description = "FQDN du serveur SQL"
}

output "cosmosdb_endpoint" {
  value       = azurerm_cosmosdb_account.main.endpoint
  description = "Endpoint CosmosDB"
}

output "redis_hostname" {
  value       = azurerm_redis_cache.main.hostname
  description = "Hostname Redis"
}

output "appinsights_instrumentation_key" {
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
  description = "Clé Application Insights"
}

output "resource_group_name" {
  value = azurerm_resource_group.main.name
}
