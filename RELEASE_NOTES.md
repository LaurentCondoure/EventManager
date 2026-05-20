# Release Notes

## v0.0.0 — 2026-05-13

**Events Management Platform — Local stack, full demonstration scope**

---

### Summary

V0 closes the first iteration of the events management platform. The scope covers the complete local stack: .NET 8 backend, Vue.js 3 frontend, and the full data infrastructure (SQL Server, MongoDB, Redis, Elasticsearch, Varnish) orchestrated via Docker Compose.

Cloud deployment (Azure) and continuous delivery (Azure DevOps CD) were intentionally deferred after a scoping decision: the local stack uses Varnish and Elasticsearch as containers, which do not map cleanly to the equivalent Azure managed services (Azure Front Door / Azure AI Search). Delivering a shallow Azure deployment would have undercut the demonstration value. Both pipelines and Terraform code exist in the repository; 
the V1 iteration that resolves the architecture gap.

---

### What is delivered

#### Functional

| Feature | Status |
|---|---|
| Event CRUD (create, read, update, delete) | Done |
| Paginated event listing | Done |
| Full-text search with field-weighted relevance | Done |
| Event detail with aggregated comments | Done |
| Comment creation with rating (1–5) | Done |
| Statistics by category | Done |
| Health check endpoint | Done |

#### Architecture

| Layer | Technology | Role |
|---|---|---|
| HTTP cache | Varnish 7 | Caches GET responses at network edge (TTL 5 min) |
| API | ASP.NET Core .NET 8 | REST API, validation, rate limiting, output caching |
| Application cache | Redis 7 | Versioned-key cache invalidation for event lists and details |
| Structured data | SQL Server 2022 | Events — Dapper, explicit queries, migrations |
| Document data | MongoDB 7 | Comments — schema-flexible, sorted by creation date |
| Full-text search | Elasticsearch 8.11 | Multi-field query, title boosted ×2, pagination |
| Frontend | Vue.js 3 + Pinia | SPA — event list, detail, form, search, statistics chart |
| Infrastructure | Docker Compose | All services with health checks and automated SQL migrations |

#### Engineering

| Practice | Detail |
|---|---|
| Architecture Decision Records | 12 ADRs covering key decisions from repo structure to cache invalidation |
| Test coverage | 96% (tracked via Codecov) — unit, integration (Testcontainers), infrastructure |
| Test strategy | Versioned (v1.0 → v2.0), documented in TEST_STRATEGY.md |
| Error handling | Environment-aware: full detail in development, opaque message in production |
| Cache invalidation | Versioned-key strategy — O(1), atomic, cluster-compatible (ADR-006, ADR-011) |
| Rate limiting | Fixed window, 100 req/min |
| Technical debt | Documented in TECHNICAL_DEBT.md with rationale and resolution paths |
| AI usage | Transparency statement in AI_USAGE.md |

#### Infrastructure as Code

Terraform code is present for two targets:

| Target | State |
|---|---|
| `infrastructure/terraform/local/` | Validated — `terraform plan` passes, null provider |
| `infrastructure/terraform/azure/` | Code complete — not applied (cloud scope deferred) |

#### CI/CD pipelines

Four Azure DevOps pipeline files are present in the repository, path-scoped per component. They were not connected to a live organisation in V0.

| Pipeline | Trigger | Stages |
|---|---|---|
| `infrastructure/azure pipelines/azure-pipelines-backend.yml` | `backend/**` | Restore → Build → Test (≥80% coverage) → Publish artifact |
| `infrastructure/azure pipelines/azure-pipelines-frontend.yml` | `frontend/**` | Lint → Test → Build → Publish artifact |
| `infrastructure/azure pipelines/azure-pipelines-terraform.yml` | `terraform/**` | Init → Validate → Plan |
| `infrastructure/azure pipelines/azure-pipelines-cd.yml` | Manual only | Infrastructure gate → Deploy |

---

### Known limitations

| Item | Notes | Resolution path |
|---|---|---|
| No authentication | All endpoints are public | JWT + `[Authorize]` planned in V2 (see ROADMAP) |
| Rate limiter burst window | Fixed-window algorithm allows burst at renewal | Token bucket or sliding window |
| Elasticsearch not on Azure | SDK incompatibility with Azure AI Search | Azure Container Instances target documented in `terraform/ProductionTarget/` |
| Search input unbounded | No length/charset guard on `q` parameter | Input validation |
| `ReindexAllAsync` not bulk | Individual index calls — acceptable at current volume | Bulk API for scale documented in CLOSURE.md |

---

### What is not in V0 scope

- Azure deployment
- Azure DevOps connected pipelines
- Reservation system
- JWT authentication
- Angular and React frontends (see ROADMAP)

---

### Documentation index

| Document | Location |
|---|---|
| Architecture (implemented) | `documents/technical/Architecture.md` |
| Architecture Decision Records | `documents/02-adr/` (12 ADRs) |
| Data model | `documents/03-technical/DATA_MODEL.md` |
| Technical design | `documents/03-technical/TECHNICAL_DESIGN.md` |
| V0 closure | `/V0_CLOSURE.md` |
| Test strategy | `documents/03-technical/TEST_STRATEGY.md` |
| Pipelines | `documents/03-technical/PIPELINES.md` |
| AI usage | `AI_USAGE.md` |
| Roadmap | `ROADMAP.md` |

---

### Getting started

See [README.md](README.md) 

---

### Roadmap

See [ROADMAP.md](ROADMAP.md) for V1 planned evolutions: graphic identity, reservation system, authentication.
