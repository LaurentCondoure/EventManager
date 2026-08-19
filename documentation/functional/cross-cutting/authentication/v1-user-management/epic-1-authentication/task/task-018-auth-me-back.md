# TASK-018 — Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-006](../stories/story-006-session-persistence.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement `GET /auth/me` endpoint.

- Apply rate limit: fixed window, 3 requests per minute per authenticated user
- Rate limit exceeded → 429 with `Retry-After` header
- Valid access token in cookie → return `{ role, mustResetPassword }`
- Expired access token + valid refresh token → rotate tokens silently, return `{ role, mustResetPassword }` with new cookies
- No valid token → 401

---

## Implementation Notes

- Token rotation on this endpoint must use the same `ITokenService` as `POST /auth/refresh` — no duplicated logic
- Rate limit applies per authenticated user, not per IP — user identity must be extractable before the limit is checked
- 429 must not redirect to login — response only
