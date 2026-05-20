terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }

  # Uncomment to store state in Azure Storage (team / CI-CD use)
  # backend "azurerm" {
  #   resource_group_name  = "rg-terraform-state"
  #   storage_account_name = "sttfstate001"
  #   container_name       = "tfstate"
  #   key                  = "eventmanagement.tfstate"
  # }
}

provider "azurerm" {
  features {}
}

# ── Resource Group ────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
}

# ── App Service Plan ──────────────────────────────────────────────────────────

resource "azurerm_service_plan" "main" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "B1"
}

# ── App Service ───────────────────────────────────────────────────────────────

resource "azurerm_linux_web_app" "api" {
  name                = var.app_service_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
    always_on = false  # B1 does not support always_on
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT = "Production"

    # Connection strings injected from resources declared below
    "ConnectionStrings__DefaultConnection" = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=EventManagement;User Id=${var.sql_admin_username};Password=${var.sql_admin_password};TrustServerCertificate=True"
    "ConnectionStrings__Redis"             = azurerm_redis_cache.main.primary_connection_string
    "ConnectionStrings__MongoDB"           = azurerm_cosmosdb_account.main.primary_mongodb_connection_string
    # Elasticsearch — known gap: the application uses Elastic.Clients.Elasticsearch (Elastic API),
    # incompatible with Azure AI Search. Migration path: Elastic Cloud on Azure (same SDK) or
    # Azure Container Instances with the official elasticsearch:8.11.0 image.
    # See terraform/ProductionTarget/ for the target architecture.

    APPLICATIONINSIGHTS_CONNECTION_STRING  = azurerm_application_insights.main.connection_string
  }
}

# ── SQL Server + Database ─────────────────────────────────────────────────────

resource "azurerm_mssql_server" "main" {
  name                         = var.sql_server_name
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
}

resource "azurerm_mssql_database" "main" {
  name      = "EventManagement"
  server_id = azurerm_mssql_server.main.id
  sku_name  = "Basic"
}

resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# ── CosmosDB (API MongoDB) ────────────────────────────────────────────────────

resource "azurerm_cosmosdb_account" "main" {
  name                = var.cosmosdb_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  offer_type          = "Standard"
  kind                = "MongoDB"

  capabilities {
    name = "EnableServerless"
  }

  capabilities {
    name = "EnableMongo"
  }

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.main.location
    failover_priority = 0
  }
}

# ── Redis Cache ───────────────────────────────────────────────────────────────

resource "azurerm_redis_cache" "main" {
  name                = var.redis_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  capacity            = 0
  family              = "C"
  sku_name            = "Basic"
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
}

# ── Elasticsearch — gap ───────────────────────────────────────────────────────
#
# The application uses Elastic.Clients.Elasticsearch, incompatible with Azure AI Search.
# No managed search resource is provisioned in this deployment.
#
# Migration path:
#   - Elastic Cloud on Azure (Marketplace): same SDK, managed by Elastic
#   - Azure Container Instances           : docker.elastic.co/elasticsearch:8.11.0
#
# See terraform/ProductionTarget/ for the full target architecture.

# ── Storage Account ───────────────────────────────────────────────────────────

resource "azurerm_storage_account" "main" {
  name                     = var.storage_name
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# ── Application Insights ──────────────────────────────────────────────────────

resource "azurerm_application_insights" "main" {
  name                = var.appinsights_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  application_type    = "web"
}
