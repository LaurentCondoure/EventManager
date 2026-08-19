# TASK-011 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-003](../stories/story-003-password-reset-completion.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for the password reset service.

- Valid current password + compliant new password → success, flag cleared, fresh token issued
- Wrong current password → 400
- New password violates policy → 400 with policy error
- New password matches one of last 5 → 400 `PASSWORD_REUSE`
- New password same as current → 400 `PASSWORD_REUSE`
- `PasswordHistory` pruned to 5 entries after successful reset
