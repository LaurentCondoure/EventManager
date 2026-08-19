# TASK-020 â€” Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-006](../story-006-session-persistence.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for `GET /auth/me` and session restoration.

- Valid access token â†’ 200 with role and mustResetPassword
- Expired access token + valid refresh token â†’ token rotation, 200
- No valid token â†’ 401
- Rate limit exceeded â†’ 429 with Retry-After
- `mustResetPassword = true` â†’ SPA redirects to reset screen
- 401 â†’ SPA redirects to login
- 429 â†’ SPA displays error, no redirect
