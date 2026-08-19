# TASK-002 â€” Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-001](../story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement rate limiting on `POST /auth/login` via .NET 8 `RateLimiterMiddleware`.

- Fixed window policy: 5 requests per minute per IP
- Fixed window policy: 5 requests per minute per email
- Either limit exceeded â†’ 429 with `Retry-After` header

---

## Implementation Notes

- Two independent policies must be applied â€” both checked on every request
- Email-based policy requires extracting email from request body before the rate limiter runs. The request body is a forward-only stream in ASP.NET Core â€” call `HttpContext.Request.EnableBuffering()` before reading, extract the email, then reset the stream position to 0 so the controller can read the body again
- 429 response must include `Retry-After` header indicating seconds until window resets
- Single API instance in V1 â€” .NET 8 built-in `RateLimiterMiddleware` is sufficient, no distributed storage required

**Architectural reference:** [ADR-019](../../../../../../architecture/adr/security/adr-019-rate-limiting-auth-endpoints.md)
