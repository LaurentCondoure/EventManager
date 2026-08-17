# ADR-013: Containerisation strategy

**Status:** Accepted — updated by ADR-018
**Version:** V1 — User Management & Containerisation
**Date:** 10/08/2026

---

## Context

The POC already containerises SQL Server, MongoDB, Redis, Elasticsearch, and Varnish via official images, with health checks declared on each service. The API and frontend run as bare processes — there is no single-command startup and no guarantee that the local configuration matches a production deployment. The V1 acceptance criteria require a single-command startup via containers, with identical images locally and in production.

The API (ASP.NET Core) and the frontend (Vue.js 3 / Vite) have no official Docker image — they require a Dockerfile authored and maintained in the repository.

## Options considered

**Option A — Docker + Docker Compose**
Standard multi-container local orchestration. One service per component. Same images in dev and prod. Well-supported by the .NET and Vue.js ecosystems.
Disadvantages: single-node orchestrator only.

**Option B — Kubernetes (kind / minikube locally)**
Production-grade multi-node orchestration.
Disadvantages: significant operational overhead unjustified at current scale and team size; steep learning curve for a first production release.

**Option C — Bare Docker without Compose**
Possible, but requires manual container networking and startup ordering. No benefit over Compose at this scale.

## Decision

Docker + Docker Compose. One service per runtime component. A shared base `docker-compose.yml`, with `docker-compose.override.yml` for dev-specific settings (volume mounts, exposed ports) and `docker-compose.prod.yml` for production overrides. Infrastructure components use official images. The API and frontend each have a dedicated Dockerfile in the repository.

**Health checks and startup order.**
Health checks are declared on all infrastructure services in `docker-compose.yml` (SQL Server, Redis, MongoDB, Elasticsearch). They guarantee that `docker compose ps` reflects genuine service readiness, not merely process startup.

Following ADR-018, the `sql-init` service is removed. Schema initialisation for both databases (`EventManager` and `EventManager_Identity`) is handled by EF Core migrations applied at API startup. The `api` service declares the following dependencies:

```yaml
api:
  depends_on:
    sqlserver:
      condition: service_healthy
    redis:
      condition: service_healthy
```

**API startup sequence (updated by ADR-018):**

```
SQL Server healthy
  → API starts
    → Migrate() — EventManager
    → Migrate() — EventManager_Identity
      → Idempotent seed (ADR-017)
        → API serves requests
```

## Consequences

ISO dev/prod compliance: identical images in both environments. Operational overhead is proportionate to team size and current volume. Single-command startup is reliable — no race conditions on cold start. Topology is simplified: `sql-init` removed, schema ownership consolidated in the API.

## Accepted limitations

Docker Compose supports single-node deployment only.
