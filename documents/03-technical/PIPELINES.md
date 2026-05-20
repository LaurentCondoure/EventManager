# Azure DevOps Pipelines

## Creating pipelines in Azure DevOps

Pipeline YAML files are not picked up automatically — each pipeline must be registered once
in the Azure DevOps interface.

For each of the four files below, repeat:
*Pipelines → New pipeline → Azure Repos Git → select the repository →
**Existing Azure Pipelines YAML file** → select the file → Save*

| File | Pipeline name (suggested) |
|---|---|
| `azure-pipelines-backend.yml` | `azure-pipelines-backend` |
| `azure-pipelines-frontend.yml` | `azure-pipelines-frontend` |
| `azure-pipelines-terraform.yml` | `azure-pipelines-terraform` |
| `azure-pipelines-cd.yml` | `azure-pipelines-cd` |

The pipeline names must match the `source:` values declared in `azure-pipelines-cd.yml`
so that the CD pipeline can reference the CI artifacts correctly.

---

## Overview

Four pipelines: two CI pipelines for code quality, one infrastructure CI pipeline, and one CD pipeline for deployment.

| Pipeline | Trigger | Stages | Role |
|---|---|---|---|
| `azure-pipelines-backend.yml` | push to `backend/**` | Build → Test → Publish | CI |
| `azure-pipelines-frontend.yml` | push to `frontend/**` | Lint → Test → Build | CI |
| `azure-pipelines-terraform.yml` | push to `terraform/**` | Validate → Plan | Infra CI |
| `azure-pipelines-cd.yml` | CI pipeline completion | Infrastructure → Deploy API + Deploy Frontend | CD |

All pipelines run on a **self-hosted agent** (`pool: name: Default`).

---

## Pipeline flow

```
backend/** push ──→ [backend CI] Build → Test → Publish ──→ artifact: api ──┐
                                                                              ├──→ [CD] Infrastructure → Deploy API
frontend/** push ──→ [frontend CI] Lint → Test → Build ──→ artifact: ui ────┤       (manual approval)   Deploy Frontend
                                                                              │
terraform/** push ──→ [infra CI] Validate → Plan ──→ artifact: tfplan.json   │
                                                                              │
                      The CD pipeline fetches the api and ui artifacts ───────┘
                      from the CI pipelines that triggered it.
```

The CD pipeline is triggered automatically when either CI pipeline completes on `main`. It downloads the artifacts produced by the triggering pipelines, applies infrastructure changes (gated by a manual approval), and deploys both application tiers.

---

## Prerequisites

### Self-hosted agent

A self-hosted agent is required because the pipelines use local tools (.NET SDK, Node.js, Terraform CLI)
and may access local resources (SQL Server, Redis) for integration tests.

**Setup:**

1. In Azure DevOps: *Project Settings → Agent pools → Default → New agent*
2. Download and extract the agent package on the target machine
3. Configure:
   ```powershell
   .\config.cmd --url https://dev.azure.com/<org> --auth pat --token <PAT>
   ```
4. Run:
   ```powershell
   .\run.cmd
   ```

The agent must have the following tools installed:
- .NET SDK 8
- Node.js 20+
- Terraform CLI
- Azure CLI (`az`)

### Variable groups

Create a variable group named **`eventmanager-secrets`** in *Pipelines → Library*
and mark all values as secret:

| Variable | Used by | Description |
|---|---|---|
| `ARM_CLIENT_ID` | Terraform | Service principal client ID |
| `ARM_CLIENT_SECRET` | Terraform | Service principal client secret |
| `ARM_TENANT_ID` | Terraform | Azure tenant ID |
| `ARM_SUBSCRIPTION_ID` | Terraform | Azure subscription ID |
| `SQL_ADMIN_USERNAME` | Terraform | SQL Server administrator login |
| `SQL_ADMIN_PASSWORD` | Terraform | SQL Server administrator password |
| `STORAGE_NAME` | Terraform | Storage account name (globally unique) |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | CD | Static Web App deployment token |

**Create a service principal:**

```bash
az ad sp create-for-rbac --name sp-eventmanager-terraform --role Contributor \
  --scopes /subscriptions/<subscription-id>
```

The command outputs `appId` (→ `ARM_CLIENT_ID`), `password` (→ `ARM_CLIENT_SECRET`),
and `tenant` (→ `ARM_TENANT_ID`).

**Retrieve the Static Web App deployment token:**

*Azure portal → Static Web App resource → Manage deployment token*

---

## Backend pipeline — `azure-pipelines-backend.yml`

### Trigger

Any push to `main` that modifies a file under `backend/`.

### Stages

```
Build ──→ Test ──→ Publish
```

**Build**
- Restores NuGet packages
- Compiles the solution (`--configuration Release`)
- Uploads the source tree as `build-output` artifact (consumed by Test)

**Test**
- Downloads `build-output`
- Runs all test projects with `--no-build` (uses compiled binaries)
- Enforces 80% coverage threshold via `CoverageThreshold=80`
- Publishes Cobertura coverage report to Azure DevOps

**Publish**
- Runs `dotnet publish` on `EventManager.Api`
- Zips the output and uploads it as the `api` artifact
- This artifact is consumed by the CD pipeline

### Why Build and Test are separate stages

Build and Test are in separate stages so that a test failure is visually distinct from a
build failure in the Azure DevOps UI. The Test stage uses `--no-build` to consume the
output of the Build stage without recompiling.

---

## Frontend pipeline — `azure-pipelines-frontend.yml`

### Trigger

Any push to `main` that modifies a file under `frontend/`.

### Stages

```
Lint ──→ Test ──→ Build
```

**Lint**
- Installs dependencies (`npm ci` — reproducible, uses `package-lock.json`)
- Runs ESLint

**Test**
- Installs dependencies (`npm ci`)
- Runs Vitest unit tests with coverage report
- Each stage runs its own `npm ci` — sharing `node_modules` via artifact is possible
  but heavier than a fresh install on a self-hosted agent with npm cache

**Build**
- Installs dependencies (`npm ci`)
- Runs `npm run build` (Vite production build)
- Uploads `dist/` as the `ui` artifact
- This artifact is consumed by the CD pipeline

---

## Terraform pipeline — `azure-pipelines-terraform.yml`

### Trigger

Any push to `main` that modifies a file under `terraform/`.

### Stages

```
Validate ──→ Plan
```

**Validate**
- `terraform init` — downloads the azurerm provider
- `terraform validate` — checks HCL syntax and schema

**Plan**
- `terraform init`
- `terraform plan -out=tfplan` with sensitive variables injected via `-var`
- `terraform show -json tfplan > tfplan.json` — exports the plan as JSON (technical proof)
- Uploads `tfplan.json` as the `terraform-plan` artifact

### Why Apply is not here

Terraform `apply` belongs to the CD pipeline, not to this pipeline. Separating plan (CI) from
apply (CD) ensures infrastructure changes go through the same artifact and approval flow as
application deployments. The plan is a reviewable artifact; the apply requires explicit human
approval via the `production` environment gate.

### Authentication

The pipeline authenticates to Azure via a service principal.
Credentials are injected as environment variables (`ARM_*`) from the secret variable group —
they never appear in the YAML file.

```
Variable group (secret) ──→ pipeline env vars ──→ azurerm provider
```

---

## CD pipeline — `azure-pipelines-cd.yml`

### Trigger

The CD pipeline has `trigger: none` — it is never triggered by a direct push. It runs only
when the backend CI or frontend CI pipeline completes successfully on `main`.

```yaml
resources:
  pipelines:
  - pipeline: backend-ci
    source: azure-pipelines-backend
    branch: main
    trigger: true
  - pipeline: frontend-ci
    source: azure-pipelines-frontend
    branch: main
    trigger: true
```

### Stages

```
Infrastructure ──→ Deploy API   (parallel)
               └──→ Deploy Frontend
```

**Infrastructure**
- Uses a **deployment job** targeting the `production` environment
- Azure DevOps stops here and sends a notification to configured approvers
- After approval: `terraform init` + `terraform apply -auto-approve`
- No infrastructure change is applied without explicit human review

To configure the approval gate:
*Azure DevOps → Environments → production → Approvals and checks → Add → Approvals*

**Deploy API**
- Downloads the `api` artifact produced by the backend CI pipeline
- Deploys the zip package to Azure App Service via `AzureWebApp@1`
- Depends on Infrastructure; runs in parallel with Deploy Frontend

**Deploy Frontend**
- Downloads the `ui` artifact produced by the frontend CI pipeline
- Deploys the `dist/` folder to Azure Static Web Apps via `AzureStaticWebApp@0`
- Depends on Infrastructure; runs in parallel with Deploy API

### Artifact lineage

```
[backend CI] Publish stage ──→ artifact: api ──→ [CD] DeployApi stage
[frontend CI] Build stage  ──→ artifact: ui  ──→ [CD] DeployFrontend stage
```

The CD pipeline always deploys the exact artifact that was validated by the CI pipeline
that triggered it — no rebuild, no re-test.
