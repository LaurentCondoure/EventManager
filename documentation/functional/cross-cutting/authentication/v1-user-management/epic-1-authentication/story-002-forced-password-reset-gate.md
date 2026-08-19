# STORY-002 — Forced Password Reset Gate

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `high`
**Status:** `to do`

---

## User Story

As an **authenticated user**,
I want **to be automatically redirected to the password reset screen when my account has a forced reset pending**,
so that **I cannot access any other feature until I have changed my password**.

---

## Acceptance Criteria

- [ ] Any request to a protected route with `mustResetPassword = true` in token → 403 `PASSWORD_RESET_REQUIRED`
- [ ] SPA intercepts 403 `PASSWORD_RESET_REQUIRED` → redirect to reset screen regardless of role
- [ ] No other route is accessible until reset is completed
- [ ] After reset, user lands on role-appropriate interface without going through login again

---

## Edge Cases

- [ ] User manually navigates to another route while reset is pending → route guard redirects to reset screen
- [ ] User with `mustResetPassword = true` attempts to call an API route directly → 403 `PASSWORD_RESET_REQUIRED`

---

## Out of Scope

- Password reset form and submission (STORY-003)
- Force reset trigger by admin (covered in account management epic)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-001](story-001-login.md) | Story | authStore and route guards must exist before gate can be implemented |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-006](../tasks/task-006-auth-reset-gate-back.md) | back | Implement `must_reset_password` middleware |
| [TASK-007](../tasks/task-007-auth-reset-gate-front.md) | front | Implement route guard for `PASSWORD_RESET_REQUIRED` |
| [TASK-008](../tasks/task-008-auth-reset-gate-test.md) | test | Unit tests — reset gate middleware and route guard |

---

## Definition of Done

- [ ] All acceptance criteria are met
- [ ] All edge cases are handled
- [ ] All linked tasks are closed
- [ ] No regression on existing features
- [ ] Tests written and passing
- [ ] Error handling implemented on all paths
- [ ] API documentation updated if an endpoint was added or modified
- [ ] No scope creep introduced

---

## Notes

- `must_reset_password` is read from the JWT claim — no database call required at the middleware level.
- The access token carries the claim for up to 10 min after flag is set — accepted limitation documented in the architecture decision.
