variable "resource_group_name" {
  description = "Resource group name"
  type        = string
  default     = "rg-eventmanagement-prod"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "francecentral"
}

variable "app_service_plan_name" {
  type    = string
  default = "asp-eventmanagement-prod"
}

variable "app_service_name" {
  description = "Must be globally unique in Azure"
  type        = string
  default     = "app-eventmanagement-prod"
}

variable "sql_server_name" {
  description = "Must be globally unique in Azure"
  type        = string
  default     = "sql-eventmanagement-prod"
}

variable "sql_admin_username" {
  type      = string
  sensitive = true
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}

variable "cosmosdb_name" {
  description = "Must be globally unique in Azure"
  type        = string
  default     = "cosmos-eventmanagement-prod"
}

variable "redis_name" {
  description = "Must be globally unique in Azure"
  type        = string
  default     = "redis-eventmanagement-prod"
}

variable "storage_name" {
  description = "Must be globally unique in Azure (3-24 chars, lowercase letters and digits only)"
  type        = string
}

variable "appinsights_name" {
  type    = string
  default = "appi-eventmanagement-prod"
}
