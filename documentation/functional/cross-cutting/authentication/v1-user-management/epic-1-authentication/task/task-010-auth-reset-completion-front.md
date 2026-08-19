# TASK-010 — Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-003](../stories/story-003-password-reset-completion.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement password reset form.

- Fields: current password, new password, confirm new password
- Inline validation: all fields required, new password matches confirm
- On submit → `POST /auth/reset-password`
- Display specific error on 400 (wrong current password, policy violation, reuse)
- On success → redirect to role-appropriate interface

---

## Implementation Notes

- Error messages must map to specific error codes from the API — not generic
- Form must be disabled during request to prevent concurrent submissions
- Error messages sourced from a shared i18n file — no hardcoded strings in component
