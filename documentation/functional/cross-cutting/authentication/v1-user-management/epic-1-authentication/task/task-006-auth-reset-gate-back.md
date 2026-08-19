# TASK-006 — Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-002](../stories/story-002-forced-password-reset-gate.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement middleware that intercepts every authenticated request and returns 403 `PASSWORD_RESET_REQUIRED` if the `must_reset_password` claim is present in the JWT.

- Middleware runs after JWT validation
- `must_reset_password = true` in token → 403 `PASSWORD_RESET_REQUIRED`
- `POST /auth/reset-password` is exempt from this middleware
- All other routes are blocked

---

## Implementation Notes

- Middleware must be ordered correctly in the pipeline: after JWT validation, before controllers
- Exemption of `/auth/reset-password` must be explicit — not inferred from route attributes
- Claim is read from the JWT — no database call required
