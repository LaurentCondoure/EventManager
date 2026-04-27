# Test Strategy

The development of the project required the adaptation of unit tests and showed that certain portions of code could not be covered without modifying the production code.

This latter solution not being viable once the code is fully implemented and functional, it necessitated the development of an appropriate testing strategy, allowing coverage of the methods not covered by unit tests.

This document aims to describe the intended testing strategy.

## Test levels

### Unit tests
Test a **single class in isolation**. All external dependencies (repositories, cache, database) are replaced by Moq mocks. Fast, no infrastructure required.

**Scope in this project:** validators, services, cache decorator.

### Component tests
Test **how components interact with each other down to the real database** (SQL Server, MongoDB). Caches are disabled — the goal is to validate that the core business logic works correctly through the full application layer stack (controller → service → repository → real database), without infrastructure interference.

**Scope in this project:** `SqlServerEventRepository` against an in-memory SQLite database (see constraints in the workarounds section).

### Integration tests
Test the **complete infrastructure stack** using real containers (Testcontainers). SQL Server, MongoDB, Redis, and Elasticsearch are spun up as Docker containers for the duration of the test suite. Validates behaviours that cannot be exercised without the real engine — T-SQL specific syntax, MongoDB queries, TTL enforcement, Elasticsearch indexing.

**Scope in this project:** not yet implemented. Target: Testcontainers with SQL Server, MongoDB images.

### End-to-end tests (E2E)
Test the **full system** from the HTTP client to the database, including infrastructure (Varnish, Redis, Elasticsearch). Not implemented in this project.

---

## Tests implemented

| Class | Level | Tool | Notes |
|---|---|---|---|
| `CreateEventInputValidator` | Unit | xUnit + FluentValidation.TestHelper | All BR1 rules covered |
| `CreateCommentInputValidator` | Unit | xUnit + FluentValidation.TestHelper | All BR2 rules covered |
| `EventService` | Unit | xUnit + Moq | Repository mocked via `IEventRepository` |
| `CachedEventRepository` | Unit | xUnit + Moq | Redis `IDatabase` mocked |
| `SqlServerEventRepository` | Component | xUnit + SQLite in-memory | Caches disabled — see constraints below |
| `MongoDbCommentRepository` | Unit | xUnit + Moq | Find() not mockable — see constraints below |

---

## Testability workarounds

### DbConnectionFactory — SQL Server repository

`SqlServerEventRepository` depends on `IDbConnection`. To make it testable without a running SQL Server instance, a `IDbConnectionFactory` abstraction was introduced. The factory is injected and creates the connection — in production it creates a `SqlConnection`, in tests an in-memory SQLite connection.

```
Production : IDbConnectionFactory → SqlConnection → SQL Server
Tests      : IDbConnectionFactory → SqliteConnection → SQLite in-memory
```

Each test gets its own uniquely named in-memory database (`EventManagerTestDb_{Guid}`) to prevent state leaking between tests through SQLite's shared-cache mechanism. A keep-alive connection is held open for the lifetime of the test — SQLite destroys an in-memory database when the last connection closes.

### MongoDB — extension methods not mockable

Two methods in the MongoDB Driver are **extension methods**, making them invisible to Moq:

| Method | Type | Workaround |
|---|---|---|
| `Find()` | Extension on `IMongoCollectionExtensions` | Mock `FindAsync()` instead — it is on the interface and returns `IAsyncCursor<T>` directly |
| `ToListAsync()` | Extension on `IAsyncCursorSourceExtensions` | Mock `ToCursorAsync()` — called internally by `ToListAsync()` |

Changing the filter syntax (`lambda` vs `Builders<T>.Filter`) does **not** solve the issue — the problem is the method itself, not its argument.

---

## Features that require integration tests

Some behaviours cannot be validated at unit or component level and require a real infrastructure:

**SQL Server — T-SQL specific syntax**

`SqlServerEventRepository` uses T-SQL instructions not supported by SQLite:
- `OFFSET / FETCH NEXT` — pagination
- `GETUTCDATE()` — server-side UTC timestamp
- `NEWID()` — GUID generation

Component tests with SQLite cover the repository structure and mapping but **cannot validate these queries**. A real SQL Server instance (or Testcontainers with the SQL Server image) is required to test them.

**MongoDB — Find() and collection queries**

As noted above, `Find()` is an extension method and cannot be mocked. The current unit tests for `MongoDbCommentRepository` are limited. A real MongoDB instance via **Testcontainers** (`Testcontainers.MongoDB`) would allow full validation of filters, sort order, and insert behaviour without modifying production code.

**Redis — TTL and key expiration**

Unit tests with a mocked `IDatabase` cannot validate actual TTL behaviour, key expiration, or the versioned invalidation strategy under real concurrency. These require a running Redis instance.

**Elasticsearch — indexing and search**

Indexing, relevance scoring, and field boosting can only be validated against a real Elasticsearch node. No in-memory alternative exists for the official .NET client.

**Varnish — HTTP cache headers**

`X-Cache: HIT / MISS` behaviour and TTL enforcement require the full Varnish + API stack running together. This falls under E2E testing scope.
