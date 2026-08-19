# TASK-002 — Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-001](../stories/story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement rate limiting on `POST /auth/login` via .NET 8 `RateLimiterMiddleware`.

- Fixed window policy: 5 requests per minute per IP
- Fixed window policy: 5 requests per minute per email
- Either limit exceeded → 429 with `Retry-After` header

---

## Implementation Notes

- Two independent policies must be applied — both checked on every request
- Email-based policy requires extracting email from request body before the rate limiter runs. The request body is a forward-only stream in ASP.NET Core — call `HttpContext.Request.EnableBuffering()` before reading, extract the email, then reset the stream position to 0 so the controller can read the body again
- 429 response must include `Retry-After` header indicating seconds until window resets
- Single API instance in V1 — .NET 8 built-in `RateLimiterMiddleware` is sufficient, no distributed storage required
