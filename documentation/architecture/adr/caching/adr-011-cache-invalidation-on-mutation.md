# ADR-011: Cache invalidation strategy for PUT and DELETE mutations

## Status
Accepted

## Context

The application has four caching layers:

1. **Pinia** — Client-side in-memory store. Holds the event list for the lifetime of the SPA session. Mutations (`updateEvent`, `deleteEvent`) update the store in-place immediately, so the UI reflects changes without a round-trip. Read actions (`fetchEvents`) currently always refetch on mount — Pinia is not yet used as a read cache, but the invalidation contract on writes is already correct.
2. **Varnish** — HTTP cache at the network edge. Caches full GET responses for `GET /api/events` (TTL 5 min) and `GET /api/events/{id}` (TTL 10 min). All other methods (POST, PUT, DELETE) and routes (`/search`, `/full`) pass through uncached.
3. **Redis** — Application cache. Caches deserialized .NET objects with a configurable TTL (default 10 min), using a versioned key strategy (ADR-006) to invalidate paginated lists without tracking individual page keys.
4. **SQL Server** — Source of truth. Always consistent.

With the introduction of `PUT /api/events/{id}` and `DELETE /api/events/{id}`, mutations now need to propagate through all cache layers.

**Already handled:**

| Layer | Mechanism | Timing |
|---|---|---|
| Pinia | `updateEvent` replaces the event in `events[]` in-place; `deleteEvent` filters it out | Synchronous, immediate |
| Redis | `CachedEventRepository.UpdateAsync/DeleteAsync` deletes `event:{id}` and increments `events:list:version` | Synchronous, immediate |

**Open question:** how to handle **Varnish**, which retains a stale HTTP response for up to its TTL after a mutation.

## Options considered

### Option A — Passive TTL (accept stale window)

PUT and DELETE bypass Varnish by design (only GET/HEAD are cached). Redis is invalidated immediately. Varnish serves stale data until its TTL expires naturally.

- No additional components or coupling.
- Stale window is bounded and predictable (max 5–10 min).
- Redis ensures API consistency: a cache miss after TTL expiry always returns fresh data.

### Option B — Active PURGE on specific event key

After mutation, the API sends `PURGE /api/events/{id}` to Varnish. Varnish VCL allows PURGE from the internal network only. List pages expire by TTL.

- Immediate consistency for individual event detail.
- API is coupled to Varnish's internal address.
- Adds a failure path: PURGE can fail silently while the mutation succeeded.
- List pages remain stale.

### Option C — Active BAN (wildcard invalidation)

After mutation, send a BAN pattern to Varnish invalidating all `/api/events*` entries atomically.

- Full cache consistency immediately.
- Requires Varnish management port (6082) accessible from the API.
- BAN lists accumulate in Varnish memory until swept.
- Strongest coupling between API and Varnish internals.

### Option D — Event-driven invalidation via pub/sub bus

After mutation, `CachedEventRepository` publishes an invalidation event to a message bus (Redis pub/sub, already available — no new container). Each cache layer subscribes independently:

- Redis invalidation: already synchronous, no change needed.
- Varnish invalidation: a .NET `BackgroundService` subscribes and sends PURGE to Varnish asynchronously (best-effort, fire-and-forget).

```
Mutation
  → CachedEventRepository
      → SQL write
      → Redis invalidate (synchronous)
      → Publish("cache:invalidate", id)   ← fire-and-forget
              ↓
        BackgroundService (subscriber)
          → PURGE Varnish /api/events/{id}
```

The key architectural property of this model: the mutation path has no knowledge of downstream cache topology. Adding a third cache layer (CDN, regional edge cache) requires no change to the mutation code — only a new subscriber.

## Decision

**Option A** for the current state of the project.

The current scale (single instance, two active cache layers, bounded TTL) does not justify the operational overhead of active invalidation. Redis already handles application-level consistency; Varnish is a pure performance layer with a predictable and acceptable stale window.

**Option D is the documented migration path** when the system grows:

- A third cache layer appears (CDN, regional edge cache)
- Cache invalidation latency becomes measurable and impactful
- The architecture moves toward microservices with independent cache ownership

Redis pub/sub is already present in the infrastructure, making the migration low-friction: no new container, no new broker to operate.

## Consequences

### Positive

- Zero additional complexity or coupling introduced today.
- Redis consistency is guaranteed immediately on every mutation.
- Varnish stale window is bounded (max 5 min for lists, 10 min for individual events) and consistent with the existing TTL contract.
- The architectural direction (Option D) is explicit — the decision is not a dead end.

### Negative

- A deleted or updated event can be served stale from Varnish for up to its TTL.
- Acceptable for event management (organisational workflow, not real-time); would need revisiting for ticketing or inventory use cases.

## Related decisions

- ADR-004: Cache-aside pattern
- ADR-005: Decorator pattern for caching
- ADR-006: Redis list cache invalidation via versioned key
- ROADMAP.md: event-driven architecture listed as a planned evolution
