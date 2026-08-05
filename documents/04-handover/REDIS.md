# Redis — Concepts et Implémentation

**Author:** Laurent Condoure
**Date:** 2026-06-10  
**Status:** Draft
**Project:** EventManager — Cultural Events Management Application  
**Objective:** Introduces Redis and describes how it's used in the application.

## What is Redis
Redis (stands for **Re**mote **Di**ctionary **S**erver) is a key-value database which stores data directly in **memory (RAM)** by default. It serves as the application-level cache.

Example : 
```
key          →   value
"event:123"  →   {"id":"123","title":"Concert Jazz",...}
"counter"    →   42
```

## Principles
### Benefits
Redis is used in our project as a caching layer between the database and the client to speed up data access and reduce the load on the main database. The frontend never requests the cache directly — the API forwards the request to Redis.
It then addresses two concerns:
+ Performance:
    frequently used data is cached so the application can retrieve it quickly without querying the main data source, in our case SQL Server
+ Security:
    By reducing the number of connections reaching the database, this benefits the resilience of the platform. In the event of unusually high traffic, the server is shielded behind a first valve.

### Working with Redis
To work with Redis in the .NET solution, the NuGet package StackExchange.Redis is used (documentation: https://stackexchange.github.io/StackExchange.Redis/).
1. Request Handling
After being requested, the API checks Redis (cache) to see if the requested data is already available.

2. Cache Hit
The data is found in Redis and successfully retrieved — the API returns it immediately to the client.

3. Cache Miss
If the data is not present in Redis (nil returned), the request is forwarded to the main database. The database processes the request and returns the required data to the application.

4. Cache Update
After fetching data from the database, it is stored in Redis for future use. This ensures that subsequent requests for the same data can be served faster.

### Configuration

Both the connection string and the TTL are read from `RedisOptions` (`appsettings.json`, `IOptions` pattern) — neither is hardcoded:

```json
"Redis": {
  "ConnectionString": "localhost:6379",
  "TimeToLive": 10
}
```

### TTL (Time To Live)
Without a TTL, the cache would grow indefinitely and could keep serving stale (obsolete) data forever. The duration is configurable, not hardcoded — read once from `RedisOptions.TimeToLive` and reused for every write:

```csharp
// RedisOptions.TimeToLive — appsettings.json: "Redis": { "TimeToLive": 10 } (minutes)
private readonly TimeSpan _ttl = TimeSpan.FromMinutes(options.Value.TimeToLive);

await _cache.StringSetAsync("event:123", json, _ttl);
```

Currently set to 10 minutes in `appsettings.json`. Changing the cache lifetime is a config change, not a code change.

### JSON Serialization

Redis stores **bytes** — it doesn't know .NET types. You need to convert objects to string.

```csharp
// .NET Object → JSON → Redis
Event { Id = "123", Title = "Jazz Concert" }
→ {"id":"123","title":"Jazz Concert"}
→ stored under key "event:123"

// Redis → JSON → .NET Object
"event:123" → {"id":"123","title":"Jazz Concert"}
→ Event { Id = "123", Title = "Jazz Concert" }
```

#### Why JSON and not something else

| Format | Problem |
|---|---|
| Binary (BinaryFormatter) | Unreadable, deprecated in .NET 5+, versioning issues |
| XML | Verbose, much heavier |
| **JSON** | ✅ Readable, universal, lightweight, native with System.Text.Json |

JSON allows visual inspection of the cache with Redis CLI or RedisInsight.

### Errors Handling
Health check and Redis supervision have to be worked on

## Cache Invalidation Complexity

When a write operation (create, update, delete) must invalidate cached list pages, the choice of strategy has a direct impact on Redis performance.

### O(N) — SCAN pattern (rejected)

```csharp
var keys = server.KeysAsync(pattern: "events:page:*"); // O(N)
```

`SCAN` iterates the **entire Redis keyspace** to find matching keys. N is the total number of keys in the database — not the number of cached pages. Even if only 10 pages are cached, Redis walks every key to find them. As the keyspace grows (other features add keys), scan time grows proportionally and blocks other operations.

### O(1) — Versioned key (implemented)

```csharp
await _cache.StringIncrementAsync("events:list:version"); // O(1)
```

`INCR` operates on a single known key. Time is constant regardless of keyspace size or traffic volume. Old page keys become unreachable immediately and expire naturally via TTL — no scan, no pipeline, no pattern matching.

This constant-time write comes at the cost of an extra read. Because the page key embeds the version (`events:page:{page}:size:{size}:v{version}`), the API must first read the current version before it can even check for a cache hit:

```csharp
long version = (long?)await _cache.StringGetAsync(ListVersionKey) ?? 0L; // 1st round-trip
string key = PageKey(page, pageSize, version);
RedisValue cached = await _cache.StringGetAsync(key);                    // 2nd round-trip
```

Every `GetAllAsync` call costs two Redis round-trips instead of one — even on a cache hit. That's the trade-off for O(1) invalidation: the versioned key moves cost from the (rare) write path to the (frequent) read path.

| Strategy | Command | Complexity | N = |
|---|---|---|---|
| SCAN pattern | `SCAN events:page:*` | O(N) | Total keys in Redis keyspace |
| Versioned key | `INCR events:list:version` | O(1) | — (constant) |

### Two strategies, two granularities

The versioned key above only applies to **paginated list pages** (`events:page:*`). A single event's cache entry (`event:{id}`) is invalidated differently — deleted directly, on the spot:

```csharp
public async Task UpdateAsync(Event @event)
{
    await inner.UpdateAsync(@event);
    await _cache.KeyDeleteAsync(EventKey(@event.Id));   // this one key is now stale — delete it directly
    await _cache.StringIncrementAsync(ListVersionKey);  // list pages that included it may also be stale
}
```

The distinction comes down to whether the key is known in advance. `event:{id}` is a single, known key — `KeyDeleteAsync` on it is already O(1), no versioning needed. Paginated list keys are not known in advance (any number of `page`/`pageSize` combinations may be cached) — that unbounded set of keys is exactly the case the versioned key exists to solve, since deleting each of them individually would fall back to the O(N) scan rejected above.

---

## Testing

`IDatabase` (the StackExchange.Redis type used for every cache operation — `StringGetAsync`, `StringSetAsync`, `KeyDeleteAsync`, `StringIncrementAsync`) is a plain interface, fully mockable with Moq directly — no extension-method workaround needed, unlike the MongoDB driver (see `MONGODB.md`, where `Find()` and `ToListAsync()` are extension methods and have to be mocked one level down via `ToCursorAsync()`).

```csharp
private readonly Mock<IDatabase> _dbMock = new();

var redisMock = new Mock<IConnectionMultiplexer>();
redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
         .Returns(_dbMock.Object);

_sut = new CachedEventRepository(_innerMock.Object, redisMock.Object, options);
```

`EventManager.Tests/Repositories/CachedEventRepositoryTests.cs` uses this to verify cache-aside behaviour and versioned-key invalidation in isolation, without a real Redis instance.

A second layer, `EventManager.InfrastructureTests`, runs the same repository against a real `redis:7-alpine` container via Testcontainers (`RedisFixture` — see `DOCKER.md` for how that fixture and the others are structured).

## Redis issues

### Memory Management
Data is stored in memory. If not managed well, Redis will invalidate keys based on the configured eviction policy, or raise errors on write operations.
To avoid this, `maxmemory` and `maxmemory-policy` in the Redis configuration file should match the planned usage of the application.

### Network Latency and Connection Limits
Too many connections, or latency on the network, can result in rejected or very slow responses.

- On the Redis server side: set `maxclients` to a value matching the application's expected concurrent traffic.
- On the .NET side: `IConnectionMultiplexer` is already registered as a **Singleton** (`Program.cs`) — StackExchange.Redis multiplexes commands over a small number of persistent connections internally, so there is no separate connection pool to add on top of it. What's still missing is tuning that multiplexer's own resilience settings — it is currently created with a bare `ConnectionMultiplexer.Connect(connectionString)`, with no explicit `ConfigurationOptions` for connect/sync timeouts or retry behaviour.

N.B : The project doesn't require data persistence which can also be a challenge

## To Go further
### Tools
+ Redis CLI
Redis provides an additional tool that allows interaction with an instance through an interface
https://redis.io/docs/latest/develop/tools/cli/
+ RedisInsight
RedisInsight is a more friendly interface for administration and supervision
https://redis.io/fr/redis-enterprise/redisinsight/





