# TASK-017 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-005](../stories/story-005-logout.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the logout service.

- Valid refresh token → token consumed, cookies cleared, 200
- Expired refresh token → cookies cleared, 200
- No cookie present → 200, no error
