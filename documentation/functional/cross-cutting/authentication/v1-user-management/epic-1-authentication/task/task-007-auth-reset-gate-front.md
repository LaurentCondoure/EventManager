# TASK-007 â€” Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-002](../story-002-forced-password-reset-gate.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement SPA-side route guard and Axios interceptor handling for `PASSWORD_RESET_REQUIRED`.

- Route guard checks `authStore.mustResetPassword` before every navigation
- `mustResetPassword = true` â†’ redirect to reset screen
- Axios interceptor catches 403 `PASSWORD_RESET_REQUIRED` on any API response â†’ redirect to reset screen and update `authStore`

---

## Implementation Notes

- Both the route guard and the Axios interceptor must handle this case â€” guard covers navigation, interceptor covers direct API calls
- Redirect must be idempotent â€” no redirect loop if already on reset screen
