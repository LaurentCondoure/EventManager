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
Superseded by [ADR-XXX](../theme/ADR-XXX-title.md)
```

A `Deprecated` ADR carries the reason:
```markdown
## Status
Deprecated — [short reason]
```

---

## Themes

`architecture` `api` `caching` `infrastructure` `persistence` `testing`

A new theme is added by updating this list — creation of a new theme must be
a conscious decision, not an implicit one.

---

## Index

| # | Title | Status | Themes | Version |
|---|---|---|---|---|
| [ADR-001](architecture/ADR-001-repository-structure.md) | Mono-Repository Structure with Path-Scoped Pipelines | Accepted | `architecture` | V0 |
| [ADR-002](persistence/ADR-002-primary-key-strategy.md) | GUID as Primary Key Strategy | Accepted | `persistence` | V0 |
| [ADR-003](architecture/ADR-003-clean-architecture.md) | Clean Architecture for .NET Solution Structure | Accepted | `architecture` | V0 |
| [ADR-004](caching/ADR-004-cache-aside-pattern.md) | Cache-Aside Pattern for Application Caching | Accepted | `caching` | V0 |
| [ADR-005](caching/ADR-005-decorator-pattern-caching.md) | Decorator Pattern for Caching Layer | Accepted | `caching` | V0 |
| [ADR-006](caching/ADR-006-redis-list-cache-invalidation.md) | Redis List Cache Invalidation Strategy | Accepted | `caching` | V0 |
| [ADR-007](persistence/ADR-007-cross-database-orchestration.md) | Cross-Database Orchestration for Event and Comment Operations | Accepted | `persistence` `architecture` | V0 |
| [ADR-008](api/ADR-008-rate-limiter-algorithm.md) | Rate Limiting Algorithm Selection | Accepted | `api` | V0 |
| [ADR-009](api/ADR-009-category-list-source-of-truth.md) | Category List — Single Source of Truth | Accepted | `api` | V0 |
| [ADR-010](testing/ADR-010-test-database-strategy.md) | Migration from SQLite component tests to SQL Server integration tests | Accepted | `testing` | V0 |
| [ADR-011](caching/ADR-011-cache-invalidation-on-mutation.md) | Cache invalidation strategy for PUT and DELETE mutations | Accepted | `caching` `infrastructure` | V0 |
| [ADR-012](persistence/ADR-012-remove-db-connection-factory.md) | Remove IDbConnectionFactory | Accepted | `persistence` | V0 |
