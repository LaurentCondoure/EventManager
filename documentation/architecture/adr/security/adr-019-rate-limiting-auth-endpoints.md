# ADR-019: Rate limiting on authentication endpoints

**Reference:** adr-019-rate-limiting-auth-endpoints
**Status:** Accepted
**Version introduced:** V1
**Last updated:** 2026-08-19

**Supersedes:** Known limitation in ADR-014 — "No rate limiting on the login endpoint"

---

## Context

ADR-014 documented the absence of rate limiting on `POST /auth/login` as an accepted known
limitation, justified by the closed user population (50–100 organizers, a small admin tier).
During the design phase, `design-authentication.md` introduced rate limiting on two
authentication endpoints:

- `POST /auth/login`: fixed window, 5 requests/min per IP + 5 requests/min per email
- `GET /auth/me`: fixed window, 3 requests/min per authenticated user (see ADR-020)

The login rate limit closes a brute-force attack surface that is small but not negligible
even on a closed system — credential stuffing does not require a large user base to be
effective. The `/auth/me` rate limit prevents frontend bugs or refresh loops from generating
unbounded Identity Store reads at startup.

---

## Options considered

**Option A — ASP.NET Core built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`)**
Available natively from .NET 7+. Supports fixed window, sliding window, token bucket, and
concurrency policies. No additional NuGet package — this namespace is part of the ASP.NET
Core framework. Counter storage is in-memory by default — acceptable for a single-instance
deployment; requires Redis-backed storage for horizontal scaling.

**Option B — Custom middleware**
Bespoke per-endpoint logic. Reinvents the framework, harder to audit, inconsistent
enforcement surface. Rejected.

**Option C — Varnish-level rate limiting (VCL)**
Rate limiting at the HTTP cache layer. Possible, but couples a security control to a caching
component, complicates the VCL, and provides no visibility at the application layer. Rejected.

---

## Decision

Option A: ASP.NET Core built-in rate limiting middleware. Three named policies:

| Policy name | Endpoint | Window | Limit | Partition key |
|---|---|---|---|---|
| `login-ip` | POST /auth/login | 1 min fixed | 5 req | Client IP |
| `login-email` | POST /auth/login | 1 min fixed | 5 req | Email from request body |
| `auth-me-user` | GET /auth/me | 1 min fixed | 3 req | Authenticated user ID |

Both login policies apply simultaneously — a request must pass both. An exceeded limit
returns `429 Too Many Requests` with a `Retry-After` header.

Counter storage: in-memory for V1. Redis-backed storage to be evaluated if horizontal
scaling is introduced in a future version.

---

## Consequences

The known limitation in ADR-014 ("No rate limiting on the login endpoint") is closed.
`Microsoft.AspNetCore.RateLimiting` is part of the ASP.NET Core framework from .NET 7+ —
no additional NuGet package required.

---

## Accepted limitations

In-memory counter storage does not survive API restarts and is not shared across instances.
Acceptable for a single-node V1 deployment. Must be revisited if horizontal scaling is
introduced.

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-19 | Document created — V1 rate limiting on authentication endpoints |
