# TASK-013 â€” Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-004](../story-004-silent-session-renewal.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement Axios interceptor for silent session renewal.

- Intercept 401 responses caused by expired access token
- If refresh already in progress â†’ queue the original request
- If no refresh in progress â†’ trigger `POST /auth/refresh`
- Refresh succeeds â†’ retry all queued requests transparently
- Refresh fails â†’ clear `authStore`, clear cookies, redirect to login with session expiry message

---

## Implementation Notes

- Queue must be implemented as a promise array resolved or rejected on refresh outcome
- Interceptor must distinguish between 401 from expired token and 401 from other causes (e.g. insufficient permissions)
- `TOKEN_REUSE_DETECTED` response must trigger immediate redirect without retry
