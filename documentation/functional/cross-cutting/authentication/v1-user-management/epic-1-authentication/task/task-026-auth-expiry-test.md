# TASK-026 â€” Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-008](../story-008-session-expiry.md)
**Priority:** `medium`
**Status:** `to do`

---

## Description

Write unit tests for session expiry via renewal failure.

- Refresh token with `ExpiresAt` in the past â†’ 401 on `POST /auth/refresh`
- Expired renewal â†’ SPA clears authStore, redirects to login with expiry message
- Fresh visit to login â†’ no expiry message displayed
