# Production Target Architecture

This directory documents the ideal Azure infrastructure — what would have been deployed in a
professional context without cost constraints. It complements `terraform/azure/`, which assumes
the gaps of the training version (free tier, Elasticsearch unresolved).

---

## Overview

```
[Vue.js 3 SPA]
      ↓
[Azure Front Door — CDN + WAF + HTTP cache]
      ↓
[Azure App Service — ASP.NET Core .NET 8 API]
      ↓
  ├── Azure Cache for Redis
  ├── Azure SQL Database
  ├── CosmosDB (MongoDB API)
  └── Elasticsearch (ACI)  ←  same Docker image, same SDK, managed container
```

---

## Resources — Training vs Production

| Role | Training (`terraform/azure/`) | Production Target | Reason for change |
|---|---|---|---|
| Frontend | Served from App Service | **Azure Static Web Apps** | Native CDN, Git-triggered deploy, zero cost |
| HTTP cache | *(Varnish — Docker local only)* | **Azure Front Door** | Global CDN, WAF, managed SSL |
| API | `azurerm_linux_web_app` (B1) | `azurerm_linux_web_app` (P1v3) | Autoscaling, always_on |
| SQL | `azurerm_mssql_database` Basic | `azurerm_mssql_database` S1+ | 99.99% SLA, backup retention |
| Redis | `azurerm_redis_cache` Basic C0 | `azurerm_redis_cache` Standard C1 | Replication, SLA |
| MongoDB | `azurerm_cosmosdb_account` Serverless | `azurerm_cosmosdb_account` Provisioned | Predictable latency, SLA |
| Elasticsearch | ❌ gap (SDK incompatible with Azure AI Search) | **ACI** `azurerm_container_group` | Same `Elastic.Clients.Elasticsearch` SDK, no code change |
| Secrets | App Settings (plaintext) | **Azure Key Vault** | Secret rotation, audit trail |
| Observability | Application Insights | Application Insights + **Log Analytics Workspace** | Alerts, dashboards, retention |
| Docker images | N/A | **Azure Container Registry (ACR)** | CI/CD pipeline → push image → deploy |

---

## Elasticsearch on ACI

The primary reason `terraform/azure/` contains no Elasticsearch resource: the application uses
`Elastic.Clients.Elasticsearch`, which is incompatible with the Azure AI Search REST API.

This version solves the gap using **Azure Container Instances** with the official Docker image:

```hcl
resource "azurerm_container_group" "elasticsearch" {
  container {
    image  = "docker.elastic.co/elasticsearch/elasticsearch:8.11.0"
    # Same image as local Docker — zero application code change
  }
}
```

Data is persisted via an Azure Files volume mounted from the Storage Account.

---

## Varnish on ACI

Varnish runs as an ACI container. Its VCL is generated dynamically by Terraform using
`templatefile()` — the App Service hostname is injected at plan time, uploaded to Azure Files,
and mounted into the container on startup. No manual step required.

See [TERRAFORM.md](../documents/technical/TERRAFORM.md) — Dynamic Varnish VCL Generation.

---

## Azure Front Door — Replacing Varnish

Varnish is an HTTP reverse proxy running in Docker. On Azure, **Azure Front Door** serves the
same role at global scale:

| Feature | Varnish (local) | Azure Front Door |
|---|---|---|
| HTTP cache | ✅ configurable TTL | ✅ rules engine |
| X-Cache header | ✅ custom VCL | ✅ native |
| WAF | ❌ | ✅ OWASP ruleset |
| SSL/TLS | ❌ | ✅ managed certificate |
| Multi-region | ❌ | ✅ global PoP |

---

## Azure Key Vault — Secret Management

In `terraform/azure/`, connection strings are injected directly into App Settings (visible
in the Azure portal). In production:

```hcl
app_settings = {
  "ConnectionStrings__DefaultConnection" = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.sql_connection.id})"
}
```

The value never appears in plaintext — the App Service resolves it at runtime via Managed Identity.

---

## What GS1 Would Add on Top

For an enterprise context, the following would be added:

- **Azure Container Registry + AKS** — if services are containerized at scale
- **Private Endpoints** — SQL, Redis, CosmosDB reachable only via private VNet
- **Azure Policy** — automated governance and compliance
- **Terraform remote state** — Azure Storage with locking (`azurerm` backend)
- **Terraform workspaces** — isolated dev / staging / prod environments
