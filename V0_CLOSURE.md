# Close up & coming next

## Context

This project was first initiated as a training support to build skills on a specific technology stack (Varnish, Redis, Elasticsearch, MongoDB, Docker, Terraform, Azure).
The approach was deliberately optimistic: the full target architecture was defined upfront, and each technology was learned one after the orther. Their interactions with each other were discovered gradually during the implementation.

At this point, in a production-grade application, this first version would represent a low cost proof of concept. 
This document acknowledges the missing target honestly, explains why they exist, and describes what a production-grade application would look like. 

---

## POC closure

### Dev/Prod Parity

#### What was planned
Full parity between the local development environment and the Azure production environment.

#### What was implemented
- **Local**: Windows + IIS (via `Setup-IIS.ps1`)
- **Azure**: Linux App Service B1

#### The gap
Azure App Service Linux was chosen over Windows for cost reasons within the training budget. Windows hosting on Azure would have preserved IIS parity but at significantly higher cost — out of scope for V1.

#### Why it exists
Windows and IIS hosting were the initial choice for development simplicity. Linux Azure divergence was discovered later in the project, while learning Azure (and Azure pricing policy)

#### Production-grade solution
Containerise the API with Docker. The same Linux image runs locally and on Azure App Service for Containers. Parity is guaranteed at the OS level — not just the runtime.

---

### Elasticsearch — Azure Deployment Gap

#### What was planned
Elasticsearch deployed as a managed service on Azure, integrated into the Terraform pipeline.

#### What was implemented
- **Local**: Elasticsearch runs as a Docker container (`docker.elastic.co/elasticsearch:8.11.0`)
- **Azure (`terraform/azure/`)**: not deployed — Azure AI Search is incompatible with `Elastic.Clients.Elasticsearch`
- **Azure (`terraform/ProductionTarget/`)**: deployed via Azure Container Instances (ACI) with Azure Files persistence

#### The gap
`terraform/azure/` — the deployed version — has no search capability on Azure. The
`ConnectionStrings__Elasticsearch` app setting is absent. Calls to `GET /api/events/search`
would fail in a real Azure deployment.

#### Why it exists
Azure AI Search was planned as the Azure equivalent of Elasticsearch. During implementation, it became clear that Azure AI Search exposes a different REST API — the `Elastic.Clients.Elasticsearch` SDK is not compatible. Migrating the search client would require rewriting `EventSearchService`.

#### Production-grade solution
- option to favor
    - **Azure Container Instances**: same Docker image as local, documented in `terraform/ProductionTarget/`

- feasible option
    - **Elastic Cloud on Azure** (Marketplace): same SDK, fully managed, zero code change

---

### Varnish — Azure Deployment Gap

#### What was planned
Varnish deployed as the HTTP caching layer, consistent with the local setup.

#### What was implemented
- **Local**: Varnish runs as a Docker container, port 8080
- **Azure (`terraform/azure/`)**: not deployed — no Varnish resource, no HTTP cache layer
- **Azure (`terraform/ProductionTarget/`)**: deployed via ACI with dynamically generated VCL

#### The gap
`terraform/azure/` has no HTTP cache layer. The `X-Cache` headers, cache TTL, and Varnish pass-through behaviour demonstrated locally do not exist in the Azure deployment.

#### Why it exists
First, Varnish requires a VCL configuration file pointing to the backend hostname. The hostname is only known after the App Service is created by Terraform. 
Resolving this circular dependency requires `templatefile()` — a Terraform function that generates the VCL dynamically at plan time. This solution is implemented in `terraform/ProductionTarget/` but not backported to `terraform/azure/` given the training timeframe.
Then it's also a lack in Dev/Prod parity. Varnish points to the hostname of the local machine in Docker (host.docker.internal), while in Azure production, it points to the FQDN of the App Service. Moreover required configuration aren't guaranteed to be the same (http vs https, potentially health check parameters)

#### Production-grade solution
- option to favor
    - Azure Front Door is the long-term tool to use 

- feasible option
    - Use templatefile() in Terrafom `terraform/ProductionTarget/` as the reference implementation. Azure Front Door is the long-term replacement — globally distributed, zero-ops, and it eliminates the VCL management overhead entirely.

---

### Serilog → Elasticsearch Sink

#### What was planned
Structured logs indexed in Elasticsearch, queryable via Kibana for observability dashboards.
This was described in `TECHNICAL_CHOISES.md` as a direct observability axis.

#### What was implemented
Serilog writes to console and rolling file only. The Elasticsearch sink (`Elastic.Serilog.Sinks`) is not configured. Application Insights receives telemetry on Azure.

#### The gap
Log correlation between Serilog (application logs) and Elasticsearch (search index) is not wired. Kibana dashboards for endpoint latency, error rates, and request volumes are not available.

#### Why it exists
see [Elasticsearch](#Elasticsearch)
N.B : the Elasticsearch cluster to be reachable at startup.

#### Production-grade solution
In on-premise hosting, add `Elastic.Serilog.Sinks` to `EventManager.Api`. Configure it from `appsettings.json` with the Elasticsearch URL. In production, point it to the same Elasticsearch instance (ACI or Elastic Cloud) used by the search service.

---

## V0 closure decision

The decision to close V1 at this stage comes from the previously documented points. 
The current application would have been finished and deployed on Azure at high cost, unacceptable for a production-grade application.
The current scope and technologies will be documented as is.


## Production ready items to plan 

- Final architecture definition
    - applications with predictable usage hosted on-premise
    - applications with unpredictable peak usage host on Azure
    - observability
    - on-premise / cloud communication
    - Containerised API 
- ReindexAllAsync: current implementation matches with the current target volume. Bulk calls to Elasticsearch would be considered to avoid a single oversized bulk request at scale later
- Add visual identity
- New functional features



