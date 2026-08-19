# TASK-008 â€” Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-002](../story-002-forced-password-reset-gate.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the reset gate middleware and route guard.

- Request with `must_reset_password = true` â†’ 403 `PASSWORD_RESET_REQUIRED`
- Request with `must_reset_password = false` â†’ passes through
- `POST /auth/reset-password` with `must_reset_password = true` â†’ passes through
- Route guard with `mustResetPassword = true` â†’ redirects to reset screen
- Route guard with `mustResetPassword = false` â†’ allows navigation
