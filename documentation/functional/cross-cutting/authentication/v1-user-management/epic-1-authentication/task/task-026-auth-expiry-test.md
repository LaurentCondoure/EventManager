# TASK-026 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-008](../stories/story-008-session-expiry.md)
**Priority:** `medium`
**Status:** `to do`

---

## Description

Write unit tests for session expiry via renewal failure.

- Refresh token with `ExpiresAt` in the past → 401 on `POST /auth/refresh`
- Expired renewal → SPA clears authStore, redirects to login with expiry message
- Fresh visit to login → no expiry message displayed
