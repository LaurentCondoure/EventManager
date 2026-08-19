# TASK-008 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-002](../stories/story-002-forced-password-reset-gate.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the reset gate middleware and route guard.

- Request with `must_reset_password = true` → 403 `PASSWORD_RESET_REQUIRED`
- Request with `must_reset_password = false` → passes through
- `POST /auth/reset-password` with `must_reset_password = true` → passes through
- Route guard with `mustResetPassword = true` → redirects to reset screen
- Route guard with `mustResetPassword = false` → allows navigation
