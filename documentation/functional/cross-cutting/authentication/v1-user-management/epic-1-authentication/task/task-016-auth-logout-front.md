# TASK-016 — Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-005](../stories/story-005-logout.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement logout action in the SPA.

- On logout trigger → `POST /auth/logout`
- On response (any) → clear `authStore`, redirect to login page

---

## Implementation Notes

- `authStore` must be cleared even if the API call fails — client-side session must always be terminated
- Logout trigger must be accessible from all authenticated views
