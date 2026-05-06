# Technical Debt & Implementation Gaps

## Context

This project was initiated as a training support to build skills on a specific technology stack
(Varnish, Redis, Elasticsearch, MongoDB, Docker, Terraform, Azure) within a 52-hour timeframe.

The approach was deliberately optimistic: the full target architecture was defined upfront, and
each technology's real constraints were discovered during implementation. This resulted in gaps
between the initial specification and what was actually delivered.

This document acknowledges those gaps honestly, explains why they exist, and describes what a
professional implementation would look like. The goal is not to minimize the gaps but to
demonstrate that they are understood, controlled, and have a defined resolution path.

---

## 1. Dev/Prod Parity

### What was planned
Full parity between the local development environment and the Azure production environment.

### What was implemented
- **Local**: Windows + IIS (via `Setup-IIS.ps1`)
- **Azure**: Linux App Service B1

### The gap
Linux and Windows behave differently on file system case sensitivity, path separators, and
platform-specific APIs. A bug that only manifests on Linux will not be caught in local development.
This violates the 12-factor app principle of dev/prod parity.

### Professional solution
Containerise the API with Docker. The same Linux image runs locally and on Azure App Service
for Containers. Parity is guaranteed at the OS level — not just the runtime.

This is the v2 target. See **Roadmap** section.

---

## 2. Elasticsearch — Azure Deployment Gap

### What was planned
Elasticsearch deployed as a managed service on Azure, integrated into the Terraform pipeline.

### What was implemented
- **Local**: Elasticsearch runs as a Docker container (`docker.elastic.co/elasticsearch:8.11.0`)
- **Azure (`terraform/azure/`)**: not deployed — Azure AI Search is incompatible with `Elastic.Clients.Elasticsearch`
- **Azure (`terraform/ProductionTarget/`)**: deployed via Azure Container Instances (ACI) with Azure Files persistence

### The gap
`terraform/azure/` — the deployed version — has no search capability on Azure. The
`ConnectionStrings__Elasticsearch` app setting is absent. Calls to `GET /api/events/search`
would fail in a real Azure deployment.

### Why it exists
Azure AI Search was initially planned as the Azure equivalent of Elasticsearch. During
implementation, it became clear that Azure AI Search exposes a different REST API — the
`Elastic.Clients.Elasticsearch` SDK is not compatible. Migrating the search client would
require rewriting `EventSearchService`.

### Professional solution
Two options:
- **Elastic Cloud on Azure** (Marketplace): same SDK, fully managed, zero code change
- **Azure Container Instances**: same Docker image as local, documented in `terraform/ProductionTarget/`

---

## 3. Varnish — Azure Deployment Gap

### What was planned
Varnish deployed on Azure as the HTTP caching layer, consistent with the local setup.

### What was implemented
- **Local**: Varnish runs as a Docker container, port 8080
- **Azure (`terraform/azure/`)**: not deployed — no Varnish resource, no HTTP cache layer
- **Azure (`terraform/ProductionTarget/`)**: deployed via ACI with dynamically generated VCL

### The gap
`terraform/azure/` has no HTTP cache layer. The `X-Cache` headers, cache TTL, and Varnish
pass-through behaviour demonstrated locally do not exist in the Azure deployment.

### Why it exists
Varnish requires a VCL configuration file pointing to the backend hostname. The hostname is
only known after the App Service is created by Terraform. Resolving this circular dependency
requires `templatefile()` — a Terraform function that generates the VCL dynamically at plan
time. This solution is implemented in `terraform/ProductionTarget/` but not backported to
`terraform/azure/` given the training timeframe.

### Professional solution
Use `terraform/ProductionTarget/` as the reference implementation. Azure Front Door is the
long-term replacement — globally distributed, zero-ops, and it eliminates the VCL management
overhead entirely.

---

## 4. Serilog → Elasticsearch Sink

### What was planned
Structured logs indexed in Elasticsearch, queryable via Kibana for observability dashboards.
This was described in `TECHNICAL_CHOISES.md` as a direct observability axis.

### What was implemented
Serilog writes to console and rolling file only. The Elasticsearch sink (`Serilog.Sinks.Elasticsearch`)
is not configured. Application Insights receives telemetry on Azure.

### The gap
Log correlation between Serilog (application logs) and Elasticsearch (search index) is not
wired. Kibana dashboards for endpoint latency, error rates, and request volumes are not
available.

### Why it exists
The Elasticsearch sink requires the Elasticsearch cluster to be reachable at startup. Wiring
this correctly with the Docker healthcheck and the application startup sequence was deferred
to keep the training timeline on track.

### Professional solution
Add `Serilog.Sinks.Elasticsearch` to `EventManager.Api`. Configure it from `appsettings.json`
with the Elasticsearch URL. In production, point it to the same Elasticsearch instance (ACI
or Elastic Cloud) used by the search service.

---

## 5. CI/CD — Deployment Pending

### What was planned
Full CI/CD: code push → build → test → deploy to Azure App Service automatically.

### What was implemented
- Three CI pipelines: backend (Build → Test → Publish), frontend (Lint → Test → Build), Terraform (Init → Validate → Plan)
- One CD pipeline (`azure-pipelines-cd.yml`): written, architecturally complete

### Current status
The pipelines have not been executed end-to-end against a real Azure environment.
The Azure DevOps service connection, variable groups, and the `production` environment
approval gate are not yet configured. An Azure free account ($200 credit) is available
to complete this.

### Remaining steps
1. `az ad sp create-for-rbac` — create service principal
2. Create variable group `eventmanager-secrets` in Azure DevOps Library
3. Create `production` environment with approval gate in Azure DevOps
4. `terraform apply` on `terraform/azure/`
5. Deploy API via `AzureWebApp@1` task
6. Take screenshots — `terraform destroy` after validation

This is a configuration task, not a code task. All pipeline code is ready.

---

## 6. Test Coverage — Known Gap

### What was implemented
189 backend tests, 75 frontend tests. Coverage threshold: 80%.

### The gap
`UpdateEventInputValidator` has no dedicated unit tests. The validator is exercised
indirectly through the controller integration tests but is not tested in isolation.

### Professional solution
Add a `UpdateEventInputValidatorTests` class in `EventManager.Tests` covering all rules:
required fields, max lengths, date constraints, category enum, and the optional `ArtistName`.

---

## Roadmap

### v2 — API Containerisation

Containerise `EventManager.Api` with a `Dockerfile` (Linux, multi-stage build). Deploy from
Azure Container Registry to Azure App Service for Containers (or ACI).

This resolves:
- Dev/prod parity (gap 1)
- Enables a clean CD pipeline: `docker build` → `docker push` → `az webapp config container set`
- Aligns with `terraform/ProductionTarget/` architecture

Specification and acceptance criteria to be defined at v2 planning.
