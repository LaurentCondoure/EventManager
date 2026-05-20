terraform {
  required_providers {
    null = {
      source  = "hashicorp/null"
      version = "~> 3.0"
    }
    local = {
      source  = "hashicorp/local"
      version = "~> 2.0"
    }
  }
}

# ── Simulates a Resource Group ────────────────────────────────────────────────

resource "null_resource" "resource_group" {
  triggers = {
    name     = var.resource_group_name
    location = var.location
  }
}

# ── Simulates an App Service Plan ─────────────────────────────────────────────

resource "null_resource" "app_service_plan" {
  depends_on = [null_resource.resource_group]
  triggers = {
    name     = var.app_service_plan_name
    sku_tier = "Basic"
    sku_size = "B1"
    os_type  = "Linux"
  }
}

# ── Simulates an App Service ──────────────────────────────────────────────────

resource "null_resource" "app_service" {
  depends_on = [null_resource.app_service_plan]
  triggers = {
    name    = var.app_service_name
    runtime = "DOTNETCORE|8.0"
  }
}

# ── Simulates SQL Server + Database ──────────────────────────────────────────

resource "null_resource" "sql_server" {
  depends_on = [null_resource.resource_group]
  triggers = {
    name = var.sql_server_name
  }
}

resource "null_resource" "sql_database" {
  depends_on = [null_resource.sql_server]
  triggers = {
    name = "EventManagement"
    sku  = "Basic"
  }
}

# ── Simulates CosmosDB (MongoDB API) ─────────────────────────────────────────

resource "null_resource" "cosmosdb" {
  depends_on = [null_resource.resource_group]
  triggers = {
    name       = var.cosmosdb_name
    api        = "MongoDB"
    serverless = "true"
  }
}

# ── Simulates Redis Cache ─────────────────────────────────────────────────────

resource "null_resource" "redis" {
  depends_on = [null_resource.resource_group]
  triggers = {
    name     = var.redis_name
    sku_name = "Basic"
    sku_size = "C0"
  }
}

# ── Simulates Cognitive Search ────────────────────────────────────────────────

resource "null_resource" "search" {
  depends_on = [null_resource.resource_group]
  triggers = {
    name = var.search_name
    sku  = "free"
  }
}

# ── Simulates Storage Account ─────────────────────────────────────────────────

resource "null_resource" "storage" {
  depends_on = [null_resource.resource_group]
  triggers = {
    name         = var.storage_name
    account_tier = "Standard"
    replication  = "LRS"
  }
}

# ── Simulates Application Insights ───────────────────────────────────────────

resource "null_resource" "appinsights" {
  depends_on = [null_resource.resource_group]
  triggers = {
    name             = var.appinsights_name
    application_type = "web"
  }
}

# ── Generates a summary file (local proof) ────────────────────────────────────

resource "local_file" "infrastructure_summary" {
  filename = "${path.module}/infrastructure.txt"
  content  = <<-EOT
    ╔══════════════════════════════════════════════════════╗
    ║         EventManagement — Azure Infrastructure       ║
    ╚══════════════════════════════════════════════════════╝

    Resource Group    : ${null_resource.resource_group.triggers.name}
    Location          : ${null_resource.resource_group.triggers.location}

    ── Compute ──────────────────────────────────────────
    App Service Plan  : ${null_resource.app_service_plan.triggers.name} (${null_resource.app_service_plan.triggers.sku_tier} ${null_resource.app_service_plan.triggers.sku_size})
    App Service       : ${null_resource.app_service.triggers.name} (${null_resource.app_service.triggers.runtime})

    ── Data ─────────────────────────────────────────────
    SQL Server        : ${null_resource.sql_server.triggers.name}
    SQL Database      : ${null_resource.sql_database.triggers.name} (${null_resource.sql_database.triggers.sku})
    CosmosDB          : ${null_resource.cosmosdb.triggers.name} (${null_resource.cosmosdb.triggers.api} / Serverless)

    ── Cache & Search ───────────────────────────────────
    Redis Cache       : ${null_resource.redis.triggers.name} (${null_resource.redis.triggers.sku_name} ${null_resource.redis.triggers.sku_size})
    Cognitive Search  : ${null_resource.search.triggers.name} (${null_resource.search.triggers.sku})

    ── Monitoring & Storage ─────────────────────────────
    Storage Account   : ${null_resource.storage.triggers.name}
    App Insights      : ${null_resource.appinsights.triggers.name}

    ── Total resources: 9 ──────────────────────────────
  EOT
}
