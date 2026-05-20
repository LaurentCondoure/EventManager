# .NET 8 Features — Concepts et Implémentation

## Overview

Two .NET 8 features have been added to the project: **Minimal APIs** and **Rate Limiting**. Both are built into the ASP.NET Core framework — no additional packages required.

Output Caching (also a .NET 8 feature) was explicitly rejected in favour of Redis — see [TECHNICAL_CHOICES.md](../03-technical/TECHNICAL_CHOICES.md).

---

## Minimal APIs

### What is it

Minimal APIs are a lightweight alternative to MVC controllers for declaring HTTP endpoints. Instead of a controller class with action methods, an endpoint is declared inline in a single `app.MapGet(...)` call.

```csharp
// Controller style
[HttpGet("/health")]
public IActionResult Health() => Ok(new { status = "healthy" });

// Minimal API style
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

The framework handles routing, binding, and response serialization the same way as controllers. The difference is purely structural — less boilerplate, faster cold start, slightly less overhead per request.

### When to use Minimal APIs vs Controllers

| | Minimal API | Controller |
|---|---|---|
| **Endpoint complexity** | Simple, few parameters | Complex logic, many actions |
| **Cross-cutting concerns** | `.RequireRateLimiting()`, `.RequireAuthorization()` | `[Authorize]`, `[EnableRateLimiting]` attributes |
| **Validation** | Manual | FluentValidation auto-validation |
| **Grouping** | `MapGroup` | Controller class |
| **Best for** | Utility endpoints | Business resource endpoints |

### Decision in this project

Minimal APIs are reserved for **utility endpoints** — endpoints that are not tied to a business logic and do not benefit from the controller structure (filters, FluentValidation, attribute-based authorization).

Business endpoints (`/api/events`, `/api/events/{id}/comments`, etc.) remain in controllers.

### Implementation

```
EventManager.Api/
└── MinimalApiEndpoints.cs   ← extension method registered in Program.cs
```

```csharp
// MinimalApiEndpoints.cs
public static class MinimalApiEndpoints
{
    public static WebApplication MapMinimalApiEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
           .WithTags("Health")
           .WithSummary("Health check")
           .RequireRateLimiting("fixed");

        return app;
    }
}

// Program.cs
app.MapMinimalApiEndpoints();
```

### Current endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/health` | Returns API status and current UTC timestamp |
| `GET` | `/api/events/categories` | Returns categories available for event |
| `POST` | `/admin/search/reindex` | Manually trigger a full reindex of all events from SQL Server to Elasticsearch if needed |


### Future candidates

Metrics and observability endpoints (Prometheus scrape endpoint, OpenTelemetry) are natural candidates for Minimal APIs in a future iteration. They are utility endpoints with no business logic and no need for FluentValidation.

---

## Rate Limiting

### What is it

Rate limiting constrains the number of requests a client can make within a time window. Requests that exceed the limit receive a `429 Too Many Requests` response instead of being processed.

ASP.NET Core 8 ships a built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`) with four algorithms: fixed window, sliding window, token bucket, and concurrency limiter.

### Algorithms

ASP.NET Core 8 provides four rate limiting algorithms. Each targets a different threat and carries different trade-offs.

#### Fixed Window

Divides time into discrete windows of fixed duration. Each window has an independent counter reset at the start of each period.

```
|--- window 1 ---|--- window 2 ---|
0s              60s              120s

req 1–100 at t=59s ✓  (window 1 full)
req 101   at t=59s ✗  429
req 102   at t=61s ✓  (window 2 reset)
```

**Burst vulnerability:** a client can send 100 requests at `t=59s` (end of window 1) and 100 more at `t=61s` (start of window 2) — 200 requests in 2 seconds while technically respecting the 100 req/min limit.

#### Sliding Window

The window moves continuously with time. The counter reflects all requests made in the last T seconds, regardless of window boundaries — no burst is possible at boundaries because there are no boundaries.

```
At t=90s, sliding window [t=30s … t=90s]:
  requests sent at t=59s are still counted
  → 100 req/min is strictly enforced at all times
```

**Trade-off:** requires storing a timestamp per request — O(n) memory vs O(1) for fixed window, where n is the **number of requests in the current window**. Under a 100 req/min limit, each client stores up to 100 timestamps in memory; under high traffic with many clients, total memory grows proportionally to request volume. Fixed window stores one counter per partition, reset at each interval — memory is constant regardless of traffic.

#### Token Bucket

A bucket fills with tokens at a fixed rate (e.g. 10 tokens/second). Each request consumes one token. If the bucket is full, new tokens are discarded. If it is empty, the request is rejected.

```
Bucket capacity: 20 tokens
Refill rate:     10 tokens/second

Burst of 20 req at t=0s → all consume a token ✓
req 21 at t=0s           → bucket empty ✗ 429
req 22 at t=1s           → 10 new tokens available ✓
```

**Key property:** allows controlled bursts up to the bucket capacity without permanently blocking the client. Well-suited for clients that occasionally spike but are reasonable on average.

#### Concurrency Limiter

Does not limit the rate over time — limits the number of requests processed **simultaneously**. When the concurrency limit is reached, further requests queue or are rejected.

```
Concurrency limit: 5

6 simultaneous requests:
  req 1–5 → processed in parallel ✓
  req 6   → queued or rejected depending on QueueLimit
```

**Use case:** protecting slow or resource-constrained backends (database, external service) from saturation. Unlike the other algorithms, it is independent of time — 1000 very short requests can pass, while 5 long-running requests can block.

#### Comparison

| Algorithm | Protects against | Allows bursts | Memory | n = |
|---|---|---|---|---|
| Fixed Window | Average rate | Yes (at boundaries) | O(1) | — (one counter) |
| Sliding Window | Strict rate | No | O(n) | requests in window |
| Token Bucket | Rate + controlled peaks | Yes (up to bucket size) | O(1) | — (two counters) |
| Concurrency | Resource saturation | N/A | O(1) | — (one counter) |

#### Decision for this project

**Fixed window** is the right choice here. The burst-at-boundary risk is not a business concern for a demonstration project, and the O(1) memory footprint scales trivially. The other algorithms become relevant in the following scenarios:

- **Sliding window** — when rate limiting underpins billing or per-tenant SLA guarantees
- **Token bucket** — when client SDKs need to absorb legitimate traffic spikes (e.g. retry storms after a brief outage)
- **Concurrency limiter** — when protecting a specific slow downstream (e.g. Elasticsearch queries under heavy search load)

### Configuration

The rate limiter is fully configurable via `appsettings.json`. The `QueueProcessingOrder` is not exposed — it is an architectural choice (`OldestFirst`), not an operational parameter.

```json
"RateLimiter": {
  "PermitLimit": 100,
  "WindowMinutes": 1,
  "QueueLimit": 0
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `PermitLimit` | int | 100 | Maximum requests per window |
| `WindowMinutes` | int | 1 | Window duration in minutes |
| `QueueLimit` | int | 0 | Requests queued when limit is reached. `0` = immediate rejection |

### Why QueueLimit = 0

Setting `QueueLimit` to `0` means excess requests are rejected immediately with a `429`. A non-zero queue would hold requests in memory until the next window, which adds latency and memory pressure without benefit for a public API. Clients that hit the limit should back off and retry — not wait silently.

### Implementation

```
EventManager.Infrastructure/Options/
└── RateLimiterOptions.cs    ← strongly-typed configuration

EventManager.Api/
└── Program.cs               ← AddRateLimiter + UseRateLimiter
```

```csharp
// avoid conflict with Microsoft.AspNetCore.RateLimiting
using AppRateLimiterOptions = EventManager.Infrastructure.Options.RateLimiterOptions;

// Program.cs — reads config before container is built
// (IOptions<T> is not resolvable before Build(), so builder.Configuration is used directly)
AppRateLimiterOptions  rateLimiterConfig = builder.Configuration
    .GetSection(AppRateLimiterOptions.SectionName)
    .Get<AppRateLimiterOptions>() ?? new AppRateLimiterOptions();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", policy =>
    {
        policy.PermitLimit          = rateLimiterConfig.PermitLimit;
        policy.Window               = TimeSpan.FromMinutes(rateLimiterConfig.WindowMinutes);
        policy.QueueLimit           = rateLimiterConfig.QueueLimit;
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Middleware pipeline — must be before MapControllers
app.UseRateLimiter();
```

### Applying the policy

`UseRateLimiter()` in `Program.cs` activates the middleware globally — it intercepts every request. Applying the `"fixed"` policy to a specific endpoint is a separate step, done differently depending on the endpoint type:

- **Controllers** — `[EnableRateLimiting("fixed")]` attribute at class level covers all actions.
- **Minimal APIs** — `.RequireRateLimiting("fixed")` chained on each `MapGet`/`MapPost` call. There is no attribute equivalent; it must be added explicitly per endpoint.

```csharp
// Controllers — attribute at class level
[EnableRateLimiting("fixed")]
public class EventsController : ControllerBase { ... }

[EnableRateLimiting("fixed")]
public class CommentsController : ControllerBase { ... }

// Minimal APIs — chained per endpoint
app.MapGet("/health", () => ...)
   .RequireRateLimiting("fixed");

app.MapGet("/api/events/categories", () => ...)
   .RequireRateLimiting("fixed");
```

`UseRateLimiter()` and `[EnableRateLimiting]`/`.RequireRateLimiting()` are complementary: the middleware enforces the limit, the attribute or chain call selects which policy applies.

### Data flow

```
Client
  │
  ▼
UseRateLimiter middleware
  │
  ├── Under limit → forward to controller/endpoint
  │
  └── Over limit  → 429 Too Many Requests (no controller invoked)
```

### Future evolution

The current policy applies the same limit to all clients regardless of identity. In a SaaS context, rate limiting is typically per-tenant or per-API-key, with different tiers (free: 60 req/min, pro: 1000 req/min). This requires:

1. A partition key derived from the authenticated identity (`context.User.FindFirst("tenant_id")`)
2. Per-tier configuration stored in the tenant registry
3. A `PartitionedRateLimiter` replacing the current `FixedWindowLimiter`

The current implementation (single fixed policy, configurable via appsettings) is the correct foundation for this evolution.
