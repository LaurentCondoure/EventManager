# ADR 008: Rate Limiting Algorithm Selection

## Status
Accepted

## Context

The API requires protection against abusive or accidental request floods. ASP.NET Core 8 ships four rate limiting algorithms natively (`Microsoft.AspNetCore.RateLimiting`): fixed window, sliding window, token bucket, and concurrency limiter. Each targets a different threat and carries different trade-offs in accuracy, memory usage, and implementation complexity.

The project is positioned as a SaaS platform. The rate limiter must be:
- Simple to reason about and operate
- Configurable per environment without code changes
- A valid foundation for future per-tenant limiting

## Alternatives Considered

### Alternative 1: Sliding Window (rejected)

The window moves continuously with time. The counter reflects all requests in the last T seconds — no burst is possible at window boundaries.

```
At t=90s, sliding window [t=30s … t=90s]:
  requests sent at t=59s are still counted
  → 100 req/min is strictly enforced at all times
```

**Rejected because:** requires storing a timestamp per request — O(n) memory vs O(1) for fixed window. The additional accuracy is not justified at this stage: the burst-at-boundary vulnerability of fixed window (up to 2× the limit over a short interval) is not a business risk for a demonstration project and would only matter when rate limiting underpins billing or per-tenant SLA guarantees.

### Alternative 2: Token Bucket (rejected)

A bucket fills with tokens at a fixed rate. Each request consumes one token. Bursts are allowed up to the bucket capacity.

```
Bucket capacity: 20 tokens — refill: 10 tokens/second
Burst of 20 req at t=0s → all pass ✓
req 21 at t=0s           → bucket empty ✗ 429
req 22 at t=1s           → 10 new tokens ✓
```

**Rejected because:** the burst allowance is a feature here, not a bug — but it adds a second configuration parameter (refill rate vs capacity) that must be tuned carefully. For a public API without differentiated tiers, a simple request count per minute is easier to communicate to clients and to verify in tests. Token bucket becomes the right choice when clients need to absorb legitimate retry storms after a brief outage.

### Alternative 3: Concurrency Limiter (rejected)

Limits the number of requests processed simultaneously, regardless of time. Does not constrain throughput over a window.

**Rejected because:** it solves a different problem — protecting a slow downstream resource from saturation rather than limiting client request rates. It is complementary to, not a replacement for, a time-based limiter. If Elasticsearch queries become a bottleneck under heavy search load, a concurrency limiter scoped to the search endpoint would be the right addition alongside the existing fixed window limiter.

### Alternative 4: Fixed Window (chosen)

Divides time into discrete windows of fixed duration. Each window has an independent counter.

```
|--- window 1 ---|--- window 2 ---|
0s              60s              120s

req 1–100 at t=59s ✓  (window 1 full)
req 101   at t=59s ✗  429
req 102   at t=61s ✓  (window 2 resets)
```

**Chosen because:**
- O(1) memory — one counter per partition, scales to any traffic volume.
- Simple to configure and reason about — one value per dimension (limit, window, queue).
- Client-facing contract is unambiguous: "100 requests per minute".
- The burst-at-boundary risk (up to 200 requests over 2 seconds at window crossings) is acceptable for a demonstration project with no billing or SLA implications.
- Full configurability via `appsettings.json` without code changes, consistent with the `IOptions` pattern used across the project.

## Decision

The fixed window algorithm is used with a named policy `"fixed"` applied globally to all controllers and Minimal API endpoints.

Three parameters are configurable via `appsettings.json`:

```json
"RateLimiter": {
  "PermitLimit": 50,
  "WindowMinutes": 1,
  "QueueLimit": 0
}
```

`QueueProcessingOrder` is hardcoded to `OldestFirst` — it is an architectural choice, not an operational parameter, and exposing it via configuration would risk silent misconfiguration.

`QueueLimit` defaults to `0`: excess requests are rejected immediately with `429 Too Many Requests`. Queuing requests in memory adds latency without benefit for a public API — clients that exceed the limit should back off and retry.

## Consequences

### Positive

- **Predictable behaviour.** One counter per window, reset at fixed intervals — straightforward to test and monitor.
- **Operationally configurable.** Limit and window adjustable per environment without deployment.
- **SaaS-ready foundation.** Replacing the global policy with a `PartitionedRateLimiter` keyed on tenant or API key requires only a change to the `AddRateLimiter` registration — the controller attributes and Minimal API declarations remain unchanged.

### Negative

- **Burst at boundary.** A client can send up to 2× the permit limit over a short interval at window crossings. Acceptable now; mitigated by moving to sliding window if billing or SLA requirements emerge.
- **No differentiated tiers.** All clients share the same limit. Per-tenant or per-plan limits require a `PartitionedRateLimiter` and a tenant registry — identified as a future evolution.

## Related Decisions

- ADR-003: Clean Architecture — rate limiter configured at the API layer, not in the domain
