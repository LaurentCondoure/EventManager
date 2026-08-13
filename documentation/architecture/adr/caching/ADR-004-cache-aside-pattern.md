# ADR-004: Cache-Aside Pattern for Application Caching

## Status
Accepted

## Context

The `EventManager` application requires a caching strategy for event data. The chosen pattern directly impacts data consistency, cache efficiency, and the ability to test caching behavior independently.

Key requirements:
- Add caching transparently without changing existing repository interfaces
- Maintain separation of concerns between data access and caching logic
- Enable easy testing and mocking of cache behavior
- Allow runtime configuration changes (enable/disable cache)
- Support multiple cache implementations if needed

## Alternatives Considered

### Alternative 1: Write-Through

Every write is stored in both the database and the cache simultaneously. A write is considered complete only when both stores have been updated successfully.

**Rejected because:**
- Every written record is cached regardless of whether it will ever be read, leading to inefficient cache utilization.
- Error handling is more complex: a failure on either store must be handled atomically.
- Cache population is eager — useful only when read patterns are highly predictable.

### Alternative 2: Cache-Aside (chosen)

The database is the source of truth. On read: check the cache first, fall back to the database on a miss, then populate the cache. On write: update the database, then invalidate the relevant cache entries.

**Chosen because:**
- Only data that is actually read gets cached — cache utilization is efficient.
- Database consistency is guaranteed: the cache is always derived from the database, never the reverse.
- Event data changes infrequently during its lifetime, which is the ideal profile for cache-aside.

**Known limitations:**
- Cache invalidation complexity grows with the number of affected entries per write — addressed by ADR-006 for paginated lists.
- Highly volatile data would see poor cache hit rates — not the case for event data at the current scale.

### Alternative 3: Write-Behind

Writes are acknowledged immediately and persisted to the database asynchronously in the background.

**Rejected because:**
- Risk of data loss if the application crashes before the background write completes.
- Complex failure handling and difficult to debug or test reliably.

## Decision

The cache-aside pattern is applied to all event read and write operations, following the Microsoft architecture guidance:
https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside

### Implementation Example

```csharp
// Write path — update database, then invalidate cache
public async Task<Guid> CreateAsync(Event @event)
{
    // Database insert
    // Cache invalidation
    // Return id of created event
}

// Read path — check cache first, fallback to database
public async Task<Event?> GetByIdAsync(Guid id)
{
    // Get cached data
    if (cached.HasValue)
        // Return cached

    // Select from database
    var @event = await _inner.GetByIdAsync(id);

    // Populate cache
    return @event;
}
```

## Consequences

### Positive
- Cache contains only data that has been read at least once — no wasted memory on unread records.
- Database is always the source of truth — no risk of cache drift on write failures.
- Pattern is well understood and straightforward to test: cache hit, cache miss, and invalidation are independent scenarios.

### Negative
- Search cache (Elasticsearch) is not invalidated on write — users may see stale results on the search page until the TTL expires. Data is refreshed when the individual event is read directly.
- TTL must be tuned carefully: too short degrades hit rate, too long increases stale data exposure.
- A first read after a write always hits the database (cold cache) — acceptable given the read/write ratio of event data.

## Related Decisions
- ADR-005: Decorator Pattern for Caching Layer
- ADR-006: Redis List Cache Invalidation via Versioned Key
