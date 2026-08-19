# TASK-004 — Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-001](../stories/story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement role-based redirect and Pinia store initialization post-login.

- On successful login, store `role` and `mustResetPassword` in `authStore` (Pinia)
- `mustResetPassword = true` → redirect to password reset screen
- `role = organizer` → redirect to event management
- `role = admin` or `role = super_admin` → redirect to administration section

---

## Implementation Notes

- `authStore` must be the single source of truth for role and session flags
- Redirect logic must be centralized — not duplicated across components
- Route guards depend on `authStore` being correctly populated after this task
