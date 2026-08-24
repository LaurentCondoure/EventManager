# Architecture Decision Records — Index

## Status values

| Status | Meaning |
|---|---|
| `Accepted` | Decision validated and applied |
| `Deprecated` | Decision abandoned without replacement — context has changed |
| `Superseded` | Decision replaced by a more recent ADR — always includes a link to the successor |

`Draft` ADRs are never added to the index. An ADR is indexed from `Accepted` onwards.

A `Superseded` or `Deprecated` ADR is never deleted — it preserves the historical reasoning.

A `Superseded` ADR carries the following in its status section:
```markdown
## Status
Superseded by [adr-XXX](../theme/adr-XXX-title.md)
```

A `Deprecated` ADR carries the reason:
```markdown
## Status
Deprecated — [short reason]
```

---

## Themes

`architecture` `api` `caching` `infrastructure` `persistence` `security` `testing`

A new theme is added by updating this list — creation of a new theme must be
a conscious decision, not an implicit one.

---

## Index

| # | Title | Status | Themes | Version |
|---|---|---|---|---|
| [adr-001](architecture/adr-001-repository-structure.md) | Mono-Repository Structure with Path-Scoped Pipelines | Accepted | `architecture` | V0 |
| [adr-002](persistence/adr-002-primary-key-strategy.md) | GUID as Primary Key Strategy | Accepted | `persistence` | V0 |
| [adr-003](architecture/adr-003-clean-architecture.md) | Clean Architecture for .NET Solution Structure | Accepted | `architecture` | V0 |
| [adr-004](caching/adr-004-cache-aside-pattern.md) | Cache-Aside Pattern for Application Caching | Accepted | `caching` | V0 |
| [adr-005](caching/adr-005-decorator-pattern-caching.md) | Decorator Pattern for Caching Layer | Accepted | `caching` | V0 |
| [adr-006](caching/adr-006-redis-list-cache-invalidation.md) | Redis List Cache Invalidation Strategy | Accepted | `caching` | V0 |
| [adr-007](persistence/adr-007-cross-database-orchestration.md) | Cross-Database Orchestration for Event and Comment Operations | Accepted | `persistence` `architecture` | V0 |
| [adr-008](api/adr-008-rate-limiter-algorithm.md) | Rate Limiting Algorithm Selection | Accepted | `api` | V0 |
| [adr-009](api/adr-009-category-list-source-of-truth.md) | Category List — Single Source of Truth | Accepted | `api` | V0 |
| [adr-010](testing/adr-010-test-database-strategy.md) | Migration from SQLite component tests to SQL Server integration tests | Accepted | `testing` | V0 |
| [adr-011](caching/adr-011-cache-invalidation-on-mutation.md) | Cache invalidation strategy for PUT and DELETE mutations | Accepted | `caching` `infrastructure` | V0 |
| [adr-012](persistence/adr-012-remove-db-connection-factory.md) | Remove IDbConnectionFactory | Accepted | `persistence` | V0 |
| [adr-013](architecture/adr-013-containerisation-strategy.md) | Containerisation Strategy | Accepted | `architecture` `infrastructure` | V1 |
| [adr-014](security/adr-014-authentication-mechanism.md) | Authentication Mechanism | Accepted | `security` | V1 |
| [adr-015](architecture/adr-015-identity-schema-isolation.md) | Identity Schema Isolation | Accepted | `persistence` `architecture` | V1 |
| [adr-016](security/adr-016-authorisation-model.md) | Authorisation Model | Accepted | `security` | V1 |
| [adr-017](security/adr-017-first-super-admin-provisioning.md) | First Super Admin Provisioning | Accepted | `security` | V1 |
| [adr-018](persistence/adr-018-migration-dapper-to-ef-core.md) | Migration from Dapper to Entity Framework Core | Accepted | `persistence` `architecture` | V1 |
| [adr-019](security/adr-019-rate-limiting-auth-endpoints.md) | Rate limiting on authentication endpoints | Accepted | `security` | V1 |
| [adr-020](architecture/adr-020-session-restoration-endpoint.md) | Session restoration endpoint — GET /auth/me | Accepted | `architecture` `security` | V1 |
| [adr-021](security/adr-021-health-endpoint-authentication.md) | Authentication on the health endpoint — JWT or a static system API key | Accepted | `security` `api` | V1 |
