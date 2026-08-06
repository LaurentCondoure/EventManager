# Local Deployment — `null` Provider

This directory validates the Azure infrastructure design **without an Azure subscription**.
It uses the `null` provider to simulate resources and the `local` provider to write proof of
what would be created. It complements `terraform/azure/` (real deployment) and
`terraform/ProductionTarget/` (ideal deployment) — see [TERRAFORM.md](../../../documents/04-handover/TERRAFORM.md).

---

## Files

### `main.tf`

Declares the two required providers (`null ~> 3.0`, `local ~> 2.0`) and 9 `null_resource` blocks,
one per Azure service that `terraform/azure/` will later deploy for real:

| Resource | Simulates | Depends on |
|---|---|---|
| `null_resource.resource_group` | Resource Group | — |
| `null_resource.app_service_plan` | App Service Plan (Linux, Basic B1) | `resource_group` |
| `null_resource.app_service` | App Service (.NET 8) | `app_service_plan` |
| `null_resource.sql_server` | Azure SQL logical server | `resource_group` |
| `null_resource.sql_database` | Azure SQL Database "EventManagement" (Basic) | `sql_server` |
| `null_resource.cosmosdb` | CosmosDB, MongoDB API, Serverless | `resource_group` |
| `null_resource.redis` | Redis Cache (Basic C0) | `resource_group` |
| `null_resource.search` | Cognitive Search (Free) | `resource_group` |
| `null_resource.storage` | Storage Account (Standard LRS) | `resource_group` |
| `null_resource.appinsights` | Application Insights | `resource_group` |

Each resource has no real effect — its `triggers` block only stores metadata (name, SKU, tier…)
so the dependency graph and variable wiring can be validated exactly as they would be with `azurerm`.

A final `local_file.infrastructure_summary` resource renders those triggers into a human-readable
box-drawn summary, written to `infrastructure.txt` (see below).

### `variables.tf`

Ten string variables, one per resource name, each with a `default` so `terraform apply` can run
non-interactively:

| Variable | Default |
|---|---|
| `resource_group_name` | `rg-eventmanagement-dev` |
| `location` | `francecentral` |
| `app_service_plan_name` | `asp-eventmanagement-dev` |
| `app_service_name` | `app-eventmanagement-dev` |
| `sql_server_name` | `sql-eventmanagement-dev` |
| `cosmosdb_name` | `cosmos-eventmanagement-dev` |
| `redis_name` | `redis-eventmanagement-dev` |
| `search_name` | `search-eventmanagement-dev` |
| `storage_name` | `stevtmgmtdev001` |
| `appinsights_name` | `appi-eventmanagement-dev` |

Naming follows the Azure CAF prefix convention (`rg-`, `asp-`, `app-`, `sql-`, `cosmos-`, `redis-`,
`search-`, `appi-`); `storage_name` has no dashes since Storage Account names must be globally
unique, lowercase, alphanumeric only.

### `outputs.tf`

Four outputs exposed after `apply`, read back from the `null_resource` triggers and the
`local_file` resource:

- `resource_group_name`
- `app_service_name`
- `sql_server_name`
- `summary_file` — path to the generated `infrastructure.txt`

### `infrastructure.txt` *(generated, not committed)*

Produced by `local_file.infrastructure_summary` on `terraform apply`. It is the tangible proof
that the plan resolved correctly: all 9 resource names/SKUs rendered from their triggers, grouped
by Compute / Data / Cache & Search / Monitoring.

---

## Running

```bash
cd infrastructure/terraform/local
terraform init
terraform validate
terraform plan
terraform apply
```

## What It Validates

- Consistent Azure CAF naming across all 9 resources
- Dependency graph (`depends_on`) resolves without cycles
- Variable interpolation into resource triggers and into the summary template
- Syntactically correct HCL — a cheap, free sanity check before touching `terraform/azure/`
