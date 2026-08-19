# TASK-019 — Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-006](../stories/story-006-session-persistence.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement automatic session restoration on every page load or refresh.

- On app mount → call `GET /auth/me`
- 200 → populate `authStore` (role, mustResetPassword), apply redirect logic
- `mustResetPassword = true` → redirect to reset screen
- 401 → clear `authStore`, redirect to login
- 429 → display error message, do not redirect

---

## Implementation Notes

- `GET /auth/me` must be called before any route guard runs — app must not render until session is known
- 429 handling must be distinct from 401 — no redirect on rate limit
