# TASK-014 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-004](../stories/story-004-silent-session-renewal.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the refresh service and Axios interceptor.

- Valid refresh token → new token pair issued, old token consumed
- Expired refresh token → 401
- Consumed refresh token → all tokens revoked, 401 `TOKEN_REUSE_DETECTED`
- Concurrent requests on token expiry → single refresh call, all requests retried
- Refresh failure → authStore cleared, redirect to login
