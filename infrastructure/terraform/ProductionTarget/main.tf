terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    local = {
      source  = "hashicorp/local"
      version = "~> 2.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "sttfstate001"
    container_name       = "tfstate"
    key                  = "eventmanagement-prod.tfstate"
  }
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

# ── App Service — API ─────────────────────────────────────────────────────────

resource "azurerm_linux_web_app" "api" {
  name                = var.app_service_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
    always_on = false
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT = "Production"

    "ConnectionStrings__DefaultConnection" = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=EventManagement;User Id=${var.sql_admin_username};Password=${var.sql_admin_password};TrustServerCertificate=True"
    "ConnectionStrings__Redis"             = azurerm_redis_cache.main.primary_connection_string
    "ConnectionStrings__MongoDB"           = azurerm_cosmosdb_account.main.primary_mongodb_connection_string
    # Elasticsearch: ACI exposed on a public IP, port 9200.
    # The IP is resolved after apply — direct reference to the ACI resource.
    "ConnectionStrings__Elasticsearch"     = "http://${azurerm_container_group.elasticsearch.ip_address}:9200"

    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.main.connection_string
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

# ── Varnish VCL — dynamic generation ─────────────────────────────────────────
#
# The App Service hostname is known at compile time (derived from var.app_service_name).
# templatefile() injects the value into the template before apply.

locals {
  varnish_vcl = templatefile("${path.root}/../../varnish/azure.vcl.tpl", {
    backend_host = "${var.app_service_name}.azurewebsites.net"
    backend_port = "80"
  })
}

resource "local_file" "varnish_vcl_rendered" {
  content  = local.varnish_vcl
  filename = "${path.module}/.generated/default.vcl"
}

# ── Storage Account — volumes ACI ────────────────────────────────────────────

resource "azurerm_storage_account" "main" {
  name                     = var.storage_name
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_share" "elasticsearch_data" {
  name                 = "elasticsearch-data"
  storage_account_name = azurerm_storage_account.main.name
  quota                = 10
}

resource "azurerm_storage_share" "varnish_vcl" {
  name                 = "varnish-vcl"
  storage_account_name = azurerm_storage_account.main.name
  quota                = 1
}

resource "azurerm_storage_share_file" "varnish_vcl_file" {
  name             = "default.vcl"
  storage_share_id = azurerm_storage_share.varnish_vcl.id
  source           = local_file.varnish_vcl_rendered.filename
}

# ── Container Group — Elasticsearch ──────────────────────────────────────────

resource "azurerm_container_group" "elasticsearch" {
  name                = "aci-elasticsearch-prod"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  ip_address_type     = "Public"
  dns_name_label      = "${var.app_service_name}-es"
  os_type             = "Linux"

  container {
    name   = "elasticsearch"
    image  = "docker.elastic.co/elasticsearch/elasticsearch:8.11.0"
    cpu    = "1"
    memory = "2"

    ports {
      port     = 9200
      protocol = "TCP"
    }

    environment_variables = {
      "discovery.type"         = "single-node"
      "xpack.security.enabled" = "false"
      "ES_JAVA_OPTS"           = "-Xms512m -Xmx512m"
    }

    volume {
      name                 = "elasticsearch-data"
      mount_path           = "/usr/share/elasticsearch/data"
      storage_account_name = azurerm_storage_account.main.name
      storage_account_key  = azurerm_storage_account.main.primary_access_key
      share_name           = azurerm_storage_share.elasticsearch_data.name
    }
  }
}

# ── Container Group — Varnish ─────────────────────────────────────────────────

resource "azurerm_container_group" "varnish" {
  name                = "aci-varnish-prod"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  ip_address_type     = "Public"
  dns_name_label      = "${var.app_service_name}-varnish"
  os_type             = "Linux"

  # Varnish depends on the uploaded VCL and the created App Service.
  depends_on = [
    azurerm_storage_share_file.varnish_vcl_file,
    azurerm_linux_web_app.api
  ]

  container {
    name   = "varnish"
    image  = "varnish:7"
    cpu    = "0.5"
    memory = "0.5"

    ports {
      port     = 80
      protocol = "TCP"
    }

    volume {
      name                 = "varnish-vcl"
      mount_path           = "/etc/varnish"
      storage_account_name = azurerm_storage_account.main.name
      storage_account_key  = azurerm_storage_account.main.primary_access_key
      share_name           = azurerm_storage_share.varnish_vcl.name
    }
  }
}

# ── Application Insights ──────────────────────────────────────────────────────

resource "azurerm_application_insights" "main" {
  name                = var.appinsights_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  application_type    = "web"
}
