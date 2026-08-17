# ADR-018: Migration from Dapper to Entity Framework Core

**Status:** Accepted
**Version:** V1 — User Management & Containerisation
**Date:** 14/08/2026

---

## Context

The POC established Dapper as the data access layer, justified by ease of use and a short functional perimeter limited to event management. The justification was valid at POC stage.

V1 introduces ASP.NET Core Identity, which integrates natively with EF Core via `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Implementing Identity against a custom Dapper-based `IUserStore` / `IRoleStore` would require significant boilerplate for marginal benefit, and would result in two competing data access paradigms in the same codebase — an architectural inconsistency with no long-term justification.

Additionally, the domain complexity ahead (reservations, artists, venues, cancellation workflows, status machines) makes EF Core's migration management, change tracking, and query composition increasingly valuable as the schema evolves. Dapper's friction grows with schema complexity and relationship density.

V1 is the last moment before first production deployment — the lowest-risk window to make a stack-level change cleanly.

## Options considered

**Option A — EF Core for Identity only, Dapper for events**
Minimal change. Identity uses the standard EF Core integration; event data access remains on Dapper.
Disadvantages: two ORM paradigms permanently coexist in the codebase. Inconsistent patterns across domains. The problem is deferred, not solved.

**Option B — Full migration to EF Core**
EF Core replaces Dapper as the single data access layer across all domains. Identity uses `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. The event schema migrates to EF Core migrations. Dapper is removed from the stack entirely.
Disadvantages: migration effort on the existing event schema. Accepted: the codebase is pre-production, the event schema is small, and the effort is proportionate.

**Option C — Retain Dapper, custom Identity store**
Implement `IUserStore` and `IRoleStore` manually against Dapper. Maintains stack consistency without introducing EF Core.
Disadvantages: significant boilerplate, no framework support, high maintenance surface for a solved problem. Rejected.

## Decision

Option B: full migration to EF Core. Dapper is removed from the stack. EF Core becomes the single data access layer for all domains.

- Two `DbContext` instances: `EventManagerDbContext` (`EventManager` database) and `IdentityDbContext` (`EventManager_Identity` database), each with its own connection string and independent migration surface — consistent with ADR-015.
- Identity uses `Microsoft.AspNetCore.Identity.EntityFrameworkCore` — no custom store implementation required.
- The `sql-init` service is removed. Schema initialisation for both databases is handled by EF Core migrations applied at API startup via `Migrate()`.
- Both `Migrate()` calls execute at startup before the idempotent seed (ADR-017) and before the API begins serving requests.

**Startup sequence:**
```
SQL Server healthy
  → API starts
    → Migrate() — EventManager
    → Migrate() — EventManager_Identity
      → Idempotent seed (ADR-017)
        → API serves requests
```

**Impact on ADR-013:** `sql-init` service removed from Docker Compose. The `api` service `depends_on` is updated to retain `sqlserver: service_healthy` and remove the `sql-init` dependency.

## Consequences

- Single data access paradigm across the entire codebase.
- `sql-init` service removed — startup topology simplified.
- Dapper and `Microsoft.Data.SqlClient` removed from the technology stack reference.
- `Microsoft.EntityFrameworkCore.SqlServer` added to the technology stack reference.
- ADR-013 startup sequence updated accordingly.

## Accepted limitations

- EF Core adds abstraction over raw SQL. For complex queries, `FromSqlRaw` remains available without reintroducing Dapper.
- Performance characteristics differ from Dapper on high-frequency read paths. Acceptable at current volume. To be reassessed if read-heavy endpoints show measurable latency under load.

## Supersedes

POC baseline — Dapper as data access layer (`Microsoft.Data.SqlClient` and `Dapper` packages removed).
