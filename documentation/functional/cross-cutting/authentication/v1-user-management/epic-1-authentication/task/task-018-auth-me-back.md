# TASK-018 â€” Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-006](../story-006-session-persistence.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement `GET /auth/me` endpoint.

- Apply rate limit: fixed window, 3 requests per minute per authenticated user
- Rate limit exceeded â†’ 429 with `Retry-After` header
- Valid access token in cookie â†’ return `{ role, mustResetPassword }`
- Expired access token + valid refresh token â†’ rotate tokens silently, return `{ role, mustResetPassword }` with new cookies
- No valid token â†’ 401

---

## Implementation Notes

- Token rotation on this endpoint must use the same `ITokenService` as `POST /auth/refresh` â€” no duplicated logic
- Rate limit applies per authenticated user, not per IP â€” user identity must be extractable before the limit is checked
- 429 must not redirect to login â€” response only

**Architectural references:** [ADR-019](../../../../../../architecture/adr/security/adr-019-rate-limiting-auth-endpoints.md), [ADR-020](../../../../../../architecture/adr/architecture/adr-020-session-restoration-endpoint.md)
