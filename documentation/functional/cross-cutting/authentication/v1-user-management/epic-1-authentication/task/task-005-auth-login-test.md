# TASK-005 â€” Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-001](../story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the login service.

- Valid credentials â†’ correct token pair issued, correct response body
- Invalid credentials (wrong password) â†’ 401 generic
- Non-existent account â†’ 401 generic, same response as wrong password
- Deactivated account â†’ 401 `ACCOUNT_DEACTIVATED`
- Rate limit exceeded (IP) â†’ 429 with `Retry-After`
- Rate limit exceeded (email) â†’ 429 with `Retry-After`
- `mustResetPassword = true` â†’ flag present in response body

---

## Implementation Notes

- Tests must cover both rate limit policies independently
- Generic 401 cases must assert response body is identical â€” no leakage between cases
