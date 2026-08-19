# TASK-019 â€” Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-006](../story-006-session-persistence.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement automatic session restoration on every page load or refresh.

- On app mount â†’ call `GET /auth/me`
- 200 â†’ populate `authStore` (role, mustResetPassword), apply redirect logic
- `mustResetPassword = true` â†’ redirect to reset screen
- 401 â†’ clear `authStore`, redirect to login
- 429 â†’ display error message, do not redirect

---

## Implementation Notes

- `GET /auth/me` must be called before any route guard runs â€” app must not render until session is known
- 429 handling must be distinct from 401 â€” no redirect on rate limit
