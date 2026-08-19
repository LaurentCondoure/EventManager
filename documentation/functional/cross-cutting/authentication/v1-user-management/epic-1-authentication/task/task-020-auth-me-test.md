# TASK-020 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-006](../stories/story-006-session-persistence.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for `GET /auth/me` and session restoration.

- Valid access token → 200 with role and mustResetPassword
- Expired access token + valid refresh token → token rotation, 200
- No valid token → 401
- Rate limit exceeded → 429 with Retry-After
- `mustResetPassword = true` → SPA redirects to reset screen
- 401 → SPA redirects to login
- 429 → SPA displays error, no redirect
