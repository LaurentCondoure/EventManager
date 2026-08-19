# TASK-014 â€” Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-004](../story-004-silent-session-renewal.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the refresh service and Axios interceptor.

- Valid refresh token â†’ new token pair issued, old token consumed
- Expired refresh token â†’ 401
- Consumed refresh token â†’ all tokens revoked, 401 `TOKEN_REUSE_DETECTED`
- Concurrent requests on token expiry â†’ single refresh call, all requests retried
- Refresh failure â†’ authStore cleared, redirect to login
