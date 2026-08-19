# TASK-005 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-001](../stories/story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the login service.

- Valid credentials → correct token pair issued, correct response body
- Invalid credentials (wrong password) → 401 generic
- Non-existent account → 401 generic, same response as wrong password
- Deactivated account → 401 `ACCOUNT_DEACTIVATED`
- Rate limit exceeded (IP) → 429 with `Retry-After`
- Rate limit exceeded (email) → 429 with `Retry-After`
- `mustResetPassword = true` → flag present in response body

---

## Implementation Notes

- Tests must cover both rate limit policies independently
- Generic 401 cases must assert response body is identical — no leakage between cases
