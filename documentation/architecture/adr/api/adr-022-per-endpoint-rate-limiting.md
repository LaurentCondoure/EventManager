# ADR-022: Per-endpoint declarative rate limiting

**Reference:** adr-022-per-endpoint-rate-limiting
**Status:** Draft
**Version introduced:** V1
**Last updated:** 2026-09-01

**Supersedes:** Single shared policy `"limit"` (50 req/min, fixed window, no partition key).

---

## Context

The current rate limiting setup registers a single named policy `"limit"` — fixed window,
50 requests/min, no partition key — applied uniformly via `[EnableRateLimiting("limit")]`
on any endpoint that opts in.

This approach has two structural problems:

1. **No semantic differentiation.** A public catalogue read, an admin write, and an
   authentication endpoint share the same threshold. The limit is calibrated for none of
   them specifically.

2. **No per-rule partition key.** The shared policy cannot partition its counter by IP or
   by a request body field. ADR-019 worked around this by registering three separate named
   policies directly in C# code — one per rule, each hardcoded. As the number of endpoints
   requiring rate limiting grows, this pattern produces scattered, inconsistent registrations
   with no single place to audit or adjust thresholds, and adding any new rate-limited
   endpoint requires a code change and redeployment.

---

## Options considered

### Option A — ASP.NET Core built-in rate limiting middleware ('Microsoft.AspNetCore.RateLimiting') (rejected)
Available natively from .NET 7+. Supports fixed window, sliding window, token bucket, and
concurrency policies. No additional NuGet package — this namespace is part of the ASP.NET
Core framework. Counter storage is in-memory by default — acceptable for a single-instance
deployment; requires Redis-backed storage for horizontal scaling.

Rejected for policies needing more than one simultaneous rule with a body-derived key, such as
`login`: `AddPolicy<TPartitionKey>` accepts a `Func<HttpContext, RateLimitPartition<TPartitionKey>>`
— a single partition per policy. `PartitionedRateLimiter.CreateChained(...)` returns a
`PartitionedRateLimiter<HttpContext>`, a different, incompatible type — a composed multi-rule
limiter cannot be returned from that delegate. Separately, a policy resolver runs before model
binding, so a rule's body-derived key (e.g. `email`) is not readable from it without the same
buffering the declarative layer does not provide.

### Option B — Registry + middleware + custom attribute (chosen)
A dedicated configuration file declares all policies. A registry built at startup holds
the runtime `RateLimiter` instances keyed by policy name. A lightweight custom attribute
binds a controller action to a policy name. A custom middleware resolves the attribute from
endpoint metadata, acquires leases from all rules in the matched policy, and enforces them.
ASP.NET Core's `[EnableRateLimiting]` is not used.

---

## Decision

Option B: declarative configuration file, runtime registry, custom attribute, custom
enforcement middleware — built on `System.Threading.RateLimiting` native primitives.

### Components

**1. Configuration file — `rate-limiting-settings.json`**

Loaded alongside `appsettings.json` at startup. Declares all policies:

```json
{
  "RateLimiting": {
    "Policies": [
      {
        "PolicyName": "login",
        "Rules": [
          {
            "RuleName": "login-ip",
            "Window": "00:01:00",
            "Limit": 5,
            "Keys": [ { "Source": "ip" } ]
          },
          {
            "RuleName": "login-email",
            "Window": "00:01:00",
            "Limit": 5,
            "Keys": [ { "Source": "body", "Name": "email" } ]
          }
        ]
      },
      {
        "PolicyName": "auth-me",
        "Rules": [
          {
            "RuleName": "auth-me-user",
            "Window": "00:01:00",
            "Limit": 3,
            "Keys": [ { "Source": "user" } ]
          }
        ]
      }
    ]
  }
}
```

**2. Policy and rule model**

- A **policy** is identified by a unique `PolicyName`.
- A policy declares one or more **rules**. All rules are enforced simultaneously — a request
  must satisfy every rule in the policy to proceed.
- Each rule defines its own `Window` (TimeSpan), `Limit` (integer), and one or more `Keys`.

**Key resolution:**

Each entry in `Keys` has a `Source` and, where the source needs one, a `Name` — the argument
that source resolves (a body field, a claim type). `Source` selects a resolver from a fixed,
extensible registry; `Name` is left out for sources that need no argument.

| Source | Name | Behaviour |
|---|---|---|
| `"ip"` | *(none)* | Client IP (`HttpContext.Connection.RemoteIpAddress`) |
| `"user"` | *(none)* | Authenticated user ID (claim resolved from `HttpContext.User`) |
| `"body"` | field name | Named field extracted from the JSON request body |
| `"claim"` | claim type | Any claim on `HttpContext.User` — e.g. a phone number carried in the JWT |

An unrecognized `Source`, or a `body`/`claim` key missing its required `Name`, fails fast at
registry build time (startup), not on the first matching request. Adding a new source (e.g.
a header or a route value) means registering a resolver in the registry — it is not a change
to this schema.

**Composite keys:** a rule with more than one entry in `Keys` resolves each and joins them
into a single counter key, shared only by requests that match on *every* key. This differs
from multiple **rules** in a policy (each an independent counter — a request must pass all of
them): e.g. `Keys: [{ "Source": "ip" }, { "Source": "body", "Name": "email" }]` gives one
counter per (IP, email) pair, whereas the `login-ip` / `login-email` rules above give two
separate counters, one keyed on IP alone and one on email alone.

Body field extraction enables `EnableBuffering` itself, reads the body, extracts the field,
then rewinds the stream to position 0. Model binding on the endpoint is unaffected.

**3. Registry**

Built at startup from the configuration. Holds one `FixedWindowRateLimiter` instance per
rule, grouped by policy name, through `PartitionedRateLimiter.CreateChained(...)`. The registry is the runtime bridge between the configuration
file and the middleware — keyed by policy name.

**4. Custom attribute — `[RateLimit("policy-name")]`**

Applied to controller actions. Declares which policy governs the endpoint. Carries no
threshold or rule definition — those live exclusively in the configuration file.

```csharp
[RateLimit("login")]
public IActionResult Login([FromBody] LoginRequest request) { ... }
```

**5. Custom enforcement middleware**

Runs after routing, before the controller. For each request:

1. Resolves the `[RateLimit]` attribute from endpoint metadata.
2. If no attribute is present, the request passes through unaffected.
3. Looks up the policy by name in the registry.
4. Acquires a lease from chained limiter.
5. If lease is acquired, the request proceeds. All leases are released after the response is sent.
6. If any lease is denied, chained limiter releases all acquired lease immediately and the middleware
   returns `429 Too Many Requests` with a `Retry-After` header. The request does not reach    the controller.

### Migration from existing policies

| Current registration | Migration |
|---|---|
| `"limit"` (shared, 50 req/min) | Removed. All `[EnableRateLimiting("limit")]` attributes replaced by `[RateLimit("policy-name")]` with appropriate per-endpoint policies declared in the configuration file. |
| `login-ip`, `login-email` (ADR-019) | Migrated into a single `"login"` policy with two rules in the configuration file. |
| `auth-me-user` (ADR-019) | Migrated into a `"auth-me"` policy in the configuration file. |

ADR-019 remains valid as the decision to rate-limit authentication endpoints and defines
their thresholds. This ADR governs how those decisions are expressed and enforced.

---

## Consequences

- All rate limiting configuration is auditable in a single file without reading C# code.
- Adding a rate-limited endpoint requires only a new policy entry in
  `rate-limiting-settings.json` and `[RateLimit("policy-name")]` on the action — no new
  C# code.
- Threshold adjustments require only a configuration change and redeployment.
- `[EnableRateLimiting]` and ASP.NET Core's built-in rate limiting middleware are no longer
  used for endpoint-level enforcement. `System.Threading.RateLimiting` primitives
  (`FixedWindowRateLimiter`) remain the counter implementation.

---

## Accepted limitations

- **In-memory counter storage.** Counters do not survive restarts and are not shared across
  instances. Inherited from ADR-019 — acceptable for single-node V1. Must be revisited if
  horizontal scaling is introduced.
- **Fixed window algorithm only.** Consistent with ADR-008. Other algorithms can be
  introduced per-rule in a future version by extending the configuration schema and the
  registry builder.
- **Body field extraction cost.** Rules using a `body` key incur a JSON parse on each
  request. Negligible at V1 volume.
- **No fallback policy.** Endpoints without a `[RateLimit]` attribute are not rate-limited.
  This is intentional — rate limiting is opt-in and explicit.

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-09-01 | Document created — TECH-001 per-endpoint declarative rate limiting |
