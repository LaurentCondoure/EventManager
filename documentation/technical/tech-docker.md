# Docker — Local Development Environment

**Author:** Laurent Condoure
**Date:** 2026-08-10  
**Status:** Draft
**Project:** EventManager — Cultural Events Management Application  
**Objective:** Introduces Docker and describes how it's used in the application.

## Concepts

The sections below explain *why* the commands in this document work, using the actual `docker-compose.yml` as the reference — not generic Docker documentation.

### Images, containers, volumes

An **image** (`mongo:7`, `redis:7-alpine`, ...) is a read-only template. A **container** is a running instance of an image — its own writable filesystem layer, thrown away when the container is removed. A **volume** is a storage location managed by Docker, kept *outside* that writable layer, so it survives container removal.

Three services declare named volumes at the bottom of `docker-compose.yml`:

```yaml
volumes:
  sqlserver-data:
  mongodb-data:
  elasticsearch-data:
```

This is why `docker compose down` preserves data (only the containers are deleted, the volumes remain on disk) and `docker compose down -v` wipes it (the `-v` flag removes the volumes too).

Redis has **no** volume declared. That's deliberate, not an oversight: Redis here is a cache, not a source of truth — losing it on every container restart is the correct behaviour, not a bug to fix.

### Healthcheck and startup order

A `healthcheck` is a command Docker runs periodically *inside* a container to decide whether it is actually ready to do its job — not just "the process started." SQL Server's container process comes up quickly, but the SQL engine itself takes time to accept connections; the healthcheck is what tells Docker the difference:

```yaml
healthcheck:
  test: ["CMD", "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", ..., "-Q", "SELECT 1", ...]
  interval: 10s
  retries: 10
  start_period: 30s
```

`sql-init` declares `depends_on: sqlserver: condition: service_healthy` — Docker Compose blocks `sql-init` from starting until that healthcheck passes. That is the actual mechanism behind "SQL Server takes ~30 seconds to be ready, `sql-init` waits for it" — not a fixed sleep, but a real readiness check. `redis`, `mongodb`, and `elasticsearch` each have their own healthcheck for the same reason: `docker compose ps` can report genuine readiness, not just "container running."

#### The four parameters

- **`test`** — the command run inside the container. Exit code `0` = healthy, anything else = unhealthy for that attempt.
- **`interval`** — how long Docker waits between two check attempts.
- **`timeout`** — how long a single attempt is allowed to run before it's counted as a failure (protects against a hung command, not just a wrong exit code).
- **`retries`** — how many *consecutive* failures are needed before Docker marks the container `unhealthy`.
- **`start_period`** *(optional)* — an initial grace window during which failures don't count against `retries`. Needed for services whose engine takes longer to accept connections than one `interval` — without it, a slow-starting SQL Server or Elasticsearch would rack up its `retries` failures and flip `unhealthy` before it ever had a real chance to come up.

#### Per-service breakdown

| Service | Test command | interval | timeout | retries | start_period | What it actually checks |
|---|---|---|---|---|---|---|
| `sqlserver` | `sqlcmd -Q "SELECT 1"` | 10s | 5s | 10 | 30s | The SQL engine accepts a connection and executes a query — not just that the process is running |
| `redis` | `redis-cli PING` | 5s | 3s | 5 | — | Redis replies `PONG` on its client protocol |
| `mongodb` | `mongosh --eval "db.runCommand({ping:1})"` | 10s | 5s | 5 | — | The `mongod` process answers the standard MongoDB ping admin command |
| `elasticsearch` | `curl .../_cluster/health \| grep 'green\|yellow'` | 15s | 10s | 10 | 30s | Cluster status is at least `yellow` (single-node setup can never reach `green`, since that requires replica shards on another node) |
| `varnish` | *(none declared)* | — | — | — | — | See below |

`elasticsearch` is the only one using `CMD-SHELL` instead of `CMD` — its check pipes `curl` into `grep`, which needs a shell to interpret the pipe. The others use the exec form (`CMD` with an argv array): no shell involved, so no quoting/globbing surprises.

`sqlserver` and `elasticsearch` are also the two slowest-starting engines here, which is why they're the only ones with a `start_period`: without it, the first 2-3 check attempts (at 10s/15s intervals) would fail before the engine is actually up, exhausting the `retries` budget and marking the container `unhealthy` even though it was only ever "still starting."

**Varnish has no healthcheck at all.** Nothing in `docker-compose.yml` needs to block on Varnish being ready — no other service declares `depends_on: varnish`, and Varnish itself starts almost instantly (it's a thin cache layer, no data engine to warm up). `docker compose ps` will show it as `running` (no health column), which is the correct and sufficient signal here.

**What actually gets blocked by a health check:** only `sql-init`, via its explicit `depends_on: sqlserver: condition: service_healthy`. The `redis`, `mongodb`, and `elasticsearch` healthchecks exist purely for observability — `docker compose ps` reporting genuine readiness — since nothing else in this compose file declares a `depends_on` against them. In particular, **the API itself is not containerized** (it runs via `dotnet run` on the host), so it never waits on any of these healthchecks automatically — check `docker compose ps` yourself before starting the API if you want to be sure the databases are actually ready, not just running.

### Networking — service name vs `host.docker.internal`

All services in one `docker-compose.yml` join a shared network that Compose creates automatically. On that network, one service reaches another simply by using its **service name** as the hostname — Docker runs an internal DNS resolver for this. That's exactly what `sql-init` does to reach SQL Server:

```bash
sqlcmd -S sqlserver -U sa ...
#       ^^^^^^^^^ the service name from docker-compose.yml, not localhost or an IP
```

If the API itself were containerized, it would reach Redis at `redis:6379`, MongoDB at `mongodb:27017`, and so on — no special configuration needed.

`host.docker.internal` (used by Varnish) is the **exception**, not the pattern to generalize: the API runs on the host machine via `dotnet run`, not as a container in this compose file, so there is no service name for Compose to resolve. `host.docker.internal` is a special DNS name Docker provides specifically to let a container reach back out to the host machine. The `extra_hosts` entry under `varnish` only exists because Linux doesn't wire up that name by default — Docker Desktop (Windows/Mac) does it automatically.

### Port mapping — `HOST:CONTAINER`

Each `ports` entry follows the pattern `HOST:CONTAINER`, two separate port spaces:

```yaml
ports:
  - "8080:80"      # varnish   — host 8080  →  container's own port 80
  - "1433:1433"    # sqlserver — host 1433  →  container's own port 1433
```

The right-hand side is fixed by the image (Varnish listens on 80 by default, unrelated to this project). The left-hand side is only what you type in your browser or `curl` — it can be changed freely without touching anything inside the container. SQL Server, Redis, MongoDB, and Elasticsearch happen to map the same port on both sides here purely for convenience, not because it's required.

### `.env` and variable substitution

Docker Compose automatically reads a `.env` file next to `docker-compose.yml` and substitutes any `${VAR}` it finds in the compose file with the matching value — no extra configuration required for this. That's how `SA_PASSWORD` and `APP_PASSWORD` reach the containers:

```yaml
environment:
  SA_PASSWORD: "${SA_PASSWORD}"   # resolved from .env at `docker compose up` time
```

This is also why `.env` must never be committed — it holds the real secret values — while `.env.example` (committed) documents which variables are expected, with placeholder values.

## Infrastructure Tests (Testcontainers)

### What Testcontainers actually does

**Testcontainers** is a library that starts and stops real Docker containers from test code — not YAML, C#. `_container.StartAsync()` does, at test-run time, roughly what `docker compose up` does for one service: pull the image, start it, wait until it's actually ready, hand back a connection string. `DisposeAsync()` tears it down afterwards. It requires the same Docker daemon this whole document has been about — nothing new to install, but Docker must be running locally (or in CI) for `dotnet test` on `EventManager.InfrastructureTests` to pass.

**Why real containers instead of mocks:** a mock only proves the code calls the driver correctly *according to what the mock was told to expect* — it can't catch a mismatch between that expectation and how the real server actually behaves. The `GuidRepresentation` issue documented in `MONGODB.md` is a concrete example: a `Mock<IMongoCollection<T>>` would happily accept a `Guid` in any representation, because the mock doesn't serialize anything. Only a test running against a real `mongo:7` container hits the actual `BsonSerializationException` the driver raises. Testcontainers exists to catch exactly that category of bug — real protocol, real serialization, real query behaviour — while still being disposable and isolated, unlike a shared dev database.

### One fixture per service

`EventManager.InfrastructureTests/Fixtures/` has one fixture per service in `docker-compose.yml`, using the matching Testcontainers module (`Testcontainers.MsSql`, `Testcontainers.Redis`, `Testcontainers.MongoDb`, `Testcontainers.Elasticsearch` — same image tags as the compose file):

```csharp
// RedisFixture.cs — the shortest example of the pattern
public class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();

    public IConnectionMultiplexer Connection { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        await _container.DisposeAsync();
    }
}
```

Each fixture is consumed via xUnit's `IClassFixture<T>` — one container is started per **test class**, shared by every `[Fact]` in that class, and disposed once the class finishes:

```csharp
public class SqlServerEventRepositoryTests : IClassFixture<SqlServerFixture>
{
    public SqlServerEventRepositoryTests(SqlServerFixture fixture) { ... }
}
```

There is no fixture sharing *across* test classes (no `[Collection]`/`ICollectionFixture` here) — `SqlServerEventRepositoryTests` and `CachedEventRepositoryTests` each get their own SQL Server container. Since xUnit runs test classes in parallel by default, running the full `InfrastructureTests` suite starts several containers at once — expect it to be noticeably slower and more resource-hungry than the mocked unit tests, and expect several `docker ps` entries to appear and disappear during a `dotnet test` run.

`SqlServerFixture` also applies the schema itself after startup (a plain `CREATE TABLE` script run over `SqlConnection`) — the container starts genuinely empty, so each fixture is responsible for putting it in a usable state before tests run.

### The exception: `VarnishFixture`

SQL Server, Redis, MongoDB, and Elasticsearch each have an official Testcontainers module — a prebuilt `XBuilder` class that already knows how to start that specific image and report readiness. **Varnish does not.** `VarnishFixture` falls back to the generic, image-agnostic API (`ContainerBuilder`, `NetworkBuilder` from `DotNet.Testcontainers.Builders`) and builds the setup by hand:

- creates an isolated Docker network per test run (`NetworkBuilder`) — the disposable, test-scoped equivalent of the network `docker-compose.yml` creates automatically for its services;
- starts a minimal `nginx:alpine` container aliased `"backend"` on that network, returning canned JSON with the same `Cache-Control` headers the real API sends — standing in for the ASP.NET API, which isn't containerized;
- starts `varnish:7` on the same network, injecting the **same VCL rules as `varnish/default.vcl`**, pointed at `backend` instead of `host.docker.internal`;
- maps Varnish's port with `assignRandomHostPort: true` instead of a fixed `8080:80` — several test runs (or parallel test classes) can each get their own Varnish container without port collisions;
- waits on a log line (`"Child launched OK"`) instead of a `healthcheck:` block — same "is it actually ready" concern as the healthchecks in `docker-compose.yml`, expressed differently because this isn't a Compose-managed container.

This mirrors the real `docker-compose.yml` topology (Varnish → backend) closely enough to exercise the actual caching rules, without depending on the API being started separately, the way local manual testing does.

## Services

| Service | Image | Port | Container name |
|---|---|---|---|
| SQL Server 2022 | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | `eventmanager-sqlserver` |
| Redis | `redis:7-alpine` | 6379 | `eventmanager-redis` |
| MongoDB | `mongo:7` | 27017 | `eventmanager-mongodb` |
| Elasticsearch | `docker.elastic.co/elasticsearch/elasticsearch:9.0.2` | 9200 | `eventmanager-elasticsearch` |
| Varnish | `varnish:7` | 8080 | `eventmanager-varnish` |

A `sql-init` companion service automatically runs all scripts in `database/migrations/` in alphabetical order once SQL Server is healthy, then exits. Adding a new migration file is sufficient — no change to `docker-compose.yml` needed.

> **Nota bene — Elasticsearch major version upgrades:** an existing `elasticsearch-data` volume created under an older major version refuses to start under a newer one directly. Bumping `8.11.0` → `9.0.2` on a volume still holding `8.11.0` data fails at boot with `cannot upgrade a node from version [8.11.0] directly to version [9.0.2], upgrade to version [8.18.0] first` — this is Elasticsearch's own upgrade-path rule, not specific to this project. When bumping the Elasticsearch image tag, remove the stale volume first — `docker volume rm docker_elasticsearch-data` (or `docker compose down -v` to wipe everything) — then `docker compose up -d elasticsearch` starts clean. A fresh environment (new clone, CI, new machine) is unaffected, since it has no pre-existing volume.
>
> Image tags are also duplicated across `docker-compose.yml` and the Testcontainers fixtures in `EventManager.InfrastructureTests/Fixtures/` (centralised in `ContainerImages.cs` on the test side) — nothing enforces they stay in sync automatically. Update both when bumping a version.

**Note — Varnish:** Varnish proxies HTTP requests to the API running on the host machine (`host.docker.internal:5256`). The API must be started separately before Varnish can serve requests. On Linux, `host.docker.internal` is resolved via `extra_hosts: host.docker.internal:host-gateway` in `docker-compose.yml` (handled automatically on Docker Desktop for Windows/Mac).

## First-time setup

**1. Create `.env` next to `docker-compose.yml`:**

```bash
cp .env.example infrastructure/docker/.env
```

Docker Compose resolves `.env` relative to the compose file's own directory (`infrastructure/docker/`), not the directory the command happens to be run from (this project runs it as `docker compose -f infrastructure/docker/docker-compose.yml up -d` from the repository root — see [README](../../README.md)). Copying it to the repository root instead leaves every variable below unset, with no obvious error.

**2. Set both passwords in `.env`:**

- `SA_PASSWORD` — the SQL Server `sa` password, used to initialise the container. Must match the password in your user secrets connection string. Must meet SQL Server complexity requirements: uppercase, lowercase, digit, special character, minimum 8 characters (see [README](../../README.md#configuration)).
- `APP_PASSWORD` — the password for `eventmanagement_user`, the least-privilege SQL login (`db_datareader` / `db_datawriter`, not `sysadmin`) the API actually connects as day-to-day. It's created by migration `003_CreateApplicationUser.sql`, which fails — and takes `sql-init` down with it — if this is left empty.

## Starting the environment

```bash
docker compose up -d
docker compose ps   # all services should be "running" (sql-init will be "exited 0")
```

SQL Server takes ~30 seconds to be ready. `sql-init` waits for the healthcheck before running the schema script.

## Verifying services

```bash
# SQL Server
docker exec eventmanager-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${SA_PASSWORD}" -Q "SELECT @@VERSION" -C -No

# Redis
docker exec eventmanager-redis redis-cli PING

# MongoDB
docker exec eventmanager-mongodb mongosh --eval "db.runCommand({ping:1})"

# Elasticsearch
curl http://localhost:9200/_cluster/health

# Varnish — first request should be MISS, second HIT
curl.exe -I http://localhost:8080/api/events   # X-Cache: MISS
curl.exe -I http://localhost:8080/api/events   # X-Cache: HIT
```

## Re-running migrations

If you need to re-apply all migrations (e.g. after `docker compose down -v`):

```bash
docker compose up sql-init
```

## Stopping the environment

```bash
docker compose down          # stop and remove containers (data volumes preserved)
docker compose down -v       # also remove volumes (wipes all data)
```
