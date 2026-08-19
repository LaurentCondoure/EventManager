# TASK-017 â€” Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-005](../story-005-logout.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the logout service.

- Valid refresh token â†’ token consumed, cookies cleared, 200
- Expired refresh token â†’ cookies cleared, 200
- No cookie present â†’ 200, no error
