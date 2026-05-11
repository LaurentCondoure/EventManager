variable "resource_group_name" {
  description = "Nom du resource group Azure"
  type        = string
  default     = "rg-eventmanagement-dev"
}

variable "location" {
  description = "Région Azure"
  type        = string
  default     = "francecentral"
}

variable "app_service_plan_name" {
  description = "Nom du plan App Service"
  type        = string
  default     = "asp-eventmanagement-dev"
}

variable "app_service_name" {
  description = "Nom de l'App Service"
  type        = string
  default     = "app-eventmanagement-dev"
}

variable "sql_server_name" {
  description = "Nom du serveur SQL"
  type        = string
  default     = "sql-eventmanagement-dev"
}

variable "cosmosdb_name" {
  description = "Nom du compte CosmosDB"
  type        = string
  default     = "cosmos-eventmanagement-dev"
}

variable "redis_name" {
  description = "Nom du cache Redis"
  type        = string
  default     = "redis-eventmanagement-dev"
}

variable "search_name" {
  description = "Nom du service Cognitive Search"
  type        = string
  default     = "search-eventmanagement-dev"
}

variable "storage_name" {
  description = "Nom du compte de stockage (unique Azure)"
  type        = string
  default     = "stevtmgmtdev001"
}

variable "appinsights_name" {
  description = "Nom d'Application Insights"
  type        = string
  default     = "appi-eventmanagement-dev"
}
