# ADR 010: Migration from SQLite component tests to SQL Server integration tests

## Status
Accepted

## Context

As documented in [TEST_STRATEGY.md](../Technical/TEST_STRATEGY.md), `SqlServerEventRepository` was tested against an in-memory SQLite database as a temporary measure while the Docker infrastructure was being set up. This allowed the repository layer to be validated quickly via component tests without requiring a running SQL Server instance.

The trade-offs were accepted knowingly:
- A `GuidTypeHandler` was introduced to bridge the SQLite/SQL Server type difference (GUID stored as TEXT in SQLite).
- T-SQL-specific syntax (`OFFSET/FETCH NEXT`, `GETUTCDATE()`, `NEWID()`) could not be validated against SQLite — only the mapping and structure were covered.
- `DbConnectionFactory` held a reference to `Microsoft.Data.Sqlite`, marked `PrivateAssets="All"` to keep it out of the production build.

In a production context, this approach would be justified when multiple repositories are developed in parallel before Docker infrastructure is available. In this training project, it served the same purpose: validate the repository layer incrementally while the full stack was being built.

Now that Docker is operational, the temporary SQLite path is replaced by SQL Server integration tests via Testcontainers, as planned in TEST_STRATEGY.md.

## Decision

- `SqlServerEventRepository` tests move from component tests (SQLite) to integration tests (Testcontainers SQL Server).
- The migration scripts in `database/migrations/` are applied to the test container — the same scripts used in production.
- `DbConnectionFactory` is simplified to SQL Server only: the SQLite branch, `DbProvider.Sqlite`, `DbProvider.InMemorySqlite`, and `GuidTypeHandler` are removed.
- `Microsoft.Data.Sqlite` is removed from `EventManager.Infrastructure.csproj`.

## Consequences

### Positive

- Repository tests now exercise the same engine, type system, and SQL syntax as production.
- T-SQL-specific behaviour (`OFFSET/FETCH`, `GETUTCDATE()`, constraints) is validated in CI.
- The migration scripts are validated on every CI run.
- `EventManager.Infrastructure` has no SQLite dependency.

### Negative

- Infrastructure tests are slower than SQLite component tests — Testcontainers starts a SQL Server container per test run (~30s, cached after first pull).
- Docker must be available on the CI agent.

## Related Decisions

- [TEST_STRATEGY.md](../Technical/TEST_STRATEGY.md) — full test level definitions and scope
- ADR-003: Clean Architecture
