# ADR-001: Mono-Repository Structure with Path-Scoped Pipelines

## Status
Accepted

## Context
The `events-management` project is a new project composed of three independently deliverable components:
- .NET solution (`Domain`, `Infrastructure`, `Api`)
- Frontend (Vue.js 3)
- Infrastructure as Code (Terraform)

Those component are planned to be deliverd through Azure DevOps, which is impacted by the git repositories defined at the start of the project.
Two repository strategies were considered:

**Separate repositories** — one per component — enable independent delivery but introduce coordination complexity. A PR on a consumer project requires the upstream project to be built and published as a NuGet package first, creating inter-repository dependencies and slowing down the delivery cycle.

**Mono-repository** — all components in a single repository — simplifies pipeline configuration. However, each component must be built and delivered independently. This requires component-scoped pipelines triggered by path filters, so that a change in `frontend/` does not trigger a rebuild of the .NET solution.


The .NET solution currently has a single consumer (`Api`). A plausible evolution is a second API targeting external partners, with stricter security requirements (authentication, response encryption) but identical data access logic. Both APIs would read from and write to the same SQL Server database.

## Decision
We use a single repository (`events-management`) containing all components, organized by folder:

```
EventManager/
├── backend/
│   ├── EventManager.Domain/
│   ├── EventManager.Infrastructure/
│   └── EventManager.Api/
├── frontend/
├── terraform/
├── documents/
│   ├── adr/
│   ├── specifications/
│   └── technical/
├── readme.md
└── azure-pipelines.yml
```

Pipelines are scoped with path filters so that each component is built and deployed independently based on what changed:
- changes under `backend/` trigger the API pipeline
- changes under `frontend/` trigger the UI pipeline
- changes under `terraform/` trigger the IaC pipeline

`Domain` and `Infrastructure` are not extracted as NuGet packages. A second `Infrastructure` project is not created for the partner API scenario. If a second consumer emerges, extraction to a shared NuGet package will be evaluated at that point.

## Consequences
- Independent delivery of UI, API, and IaC is preserved without inter-repository coordination overhead.
- A change in `Domain` triggers a full rebuild of the .NET solution — acceptable given the single consumer and the current project scope.
- `Infrastructure` is agnostic of its consumer's security policy. It returns raw data regardless of what the consumer does with it. Differentiation between a public API and a partner API belongs in the `Api` layer: authentication mechanisms, response encryption, endpoint exposure. Duplicating `Infrastructure` would mean maintaining identical data access logic in two places, with no divergence justifying the split.
- `Infrastructure` is implicitly coupled to `Api` as its sole consumer today. This is a known and accepted constraint, with a clear evolution path: extract to a shared NuGet package when a second consumer is created.
- The repository remains simple to clone, navigate, and demonstrate in a technical interview context.
