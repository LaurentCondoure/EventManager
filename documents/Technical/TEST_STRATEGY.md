# Test Strategy

## Test levels

### Unit tests — `EventManager.Tests`

Test a **single class in isolation**. All external dependencies (repositories, cache, search, database) are replaced by Moq mocks. Fast, no infrastructure required.

**Scope:** validators, `EventService`, `CachedEventRepository`.

### Integration tests — `EventManager.IntegrationTests`

Test the **API layer end-to-end** using `WebApplicationFactory<Program>`. The real ASP.NET Core pipeline runs in memory (middleware, routing, validation, DI). All infrastructure dependencies (SQL Server, Redis, MongoDB, Elasticsearch) are replaced by in-memory fakes or Moq mocks — no Docker containers required.

**Scope:** controllers, middleware, validation pipeline, HTTP status codes, response headers (Cache-Control).

### Infrastructure tests — `EventManager.InfrastructureTests`

Test **real infrastructure behaviour** using Testcontainers — each test class spins up its own Docker container(s) for the duration of the suite. Validates behaviours that cannot be exercised without the real engine: T-SQL syntax, MongoDB queries, Redis TTL, Elasticsearch indexing and search, Varnish HTTP cache rules.

**Scope:** `SqlServerEventRepository`, `MongoDbCommentRepository`, `CachedEventRepository`, `EventSearchService`, `EventService` (cross-database orchestration), Varnish (HIT/MISS rules).

---

## Tests implemented

| Class / Feature | Level | Containers | Tests |
|---|---|---|---|
| `CreateEventInputValidator` | Unit | — | 18 |
| `CreateCommentInputValidator` | Unit | — | 6 |
| `EventService` | Unit | — | 30 |
| `CachedEventRepository` | Unit | — | 11 |
| `EventCategories` | Unit | — | 4 |
| `MongoDbCommentRepository` | Unit | — | 2 |
| `EventsController` | Integration | — | 15 |
| `CommentsController` | Integration | — | 8 |
| `SqlServerEventRepository` | Infrastructure | SQL Server | 6 |
| `MongoDbCommentRepository` | Infrastructure | MongoDB | 5 |
| `CachedEventRepository` | Infrastructure | SQL Server + Redis | 8 |
| `EventService` (cross-DB) | Infrastructure | SQL Server + MongoDB | 12 |
| `EventSearchService` | Infrastructure | Elasticsearch | 3 |
| `VarnishCacheTests` | Infrastructure | Varnish + nginx | 6 |
| **Total** | | | **134+** |

Coverage: **86%**

---

## Why three layers

| Concern | Unit | Integration | Infrastructure |
|---|---|---|---|
| Business rules (validation, service logic) | ✅ fast | — | — |
| HTTP contract (status codes, headers, routing) | — | ✅ no Docker | — |
| T-SQL specific syntax (OFFSET/FETCH, GETUTCDATE) | ❌ | ❌ | ✅ |
| MongoDB queries (filter, sort, index) | ❌ | ❌ | ✅ |
| Redis TTL and key invalidation strategy | ❌ | ❌ | ✅ |
| Elasticsearch indexing and relevance scoring | ❌ | ❌ | ✅ |
| Varnish X-Cache HIT/MISS rules | ❌ | ❌ | ✅ |

---

## Infrastructure test fixtures

Each fixture manages the Docker container lifecycle (`IAsyncLifetime`) and is shared across the test class via `IClassFixture<T>`:

| Fixture | Container | Package |
|---|---|---|
| `SqlServerFixture` | SQL Server 2022 | `Testcontainers.MsSql` |
| `MongoDbFixture` | MongoDB 7 | `Testcontainers.MongoDb` |
| `RedisFixture` | Redis 7 | `Testcontainers.Redis` |
| `ElasticsearchFixture` | Elasticsearch 9 | `Testcontainers.Elasticsearch` |
| `VarnishFixture` | Varnish 7 + nginx | `Testcontainers` (GenericContainer) |

`IAsyncLifetime` implemented on the **test class** (not the fixture) handles per-test cleanup (e.g. deleting the Elasticsearch index before each test). The container itself stays alive for the entire class — starting a Docker container for each test would be prohibitively slow.

See [XUNIT_TEST_ISOLATION.md](XUNIT_TEST_ISOLATION.md) for the detailed explanation of xUnit v3 lifecycle and isolation patterns.
