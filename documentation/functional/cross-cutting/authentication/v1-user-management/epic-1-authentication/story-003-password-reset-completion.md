# STORY-003 â€” Password Reset Completion

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `high`
**Status:** `to do`

---

## User Story

As an **authenticated user with a forced reset pending**,
I want **to reset my password**,
so that **I can recover full access to my account**.

---

## Acceptance Criteria

- [ ] Current password must be provided and verified â†’ 400 if incorrect
- [ ] New password must comply with the password policy â†’ 400 with policy error if not
- [ ] New password must not be one of the last 5 passwords â†’ 400 `PASSWORD_REUSE` if it is
- [ ] On success â†’ `must_reset_password` flag cleared, fresh token pair issued, redirect to role-appropriate interface

---

## Edge Cases

- [ ] User submits same password as current â†’ rejected as reuse (last 5 check)
- [ ] User submits valid new password but current password is wrong â†’ 400, no change applied
- [ ] Token expires during reset form interaction â†’ silent renewal handles it (STORY-004)

---

## Out of Scope

- Self-service password change without forced reset (future version)
- Password reset via email link (future version)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-001](story-001-login.md) | Story | Identity setup and token issuance must exist |
| [STORY-002](story-002-forced-password-reset-gate.md) | Story | Reset gate must be in place before reset completion is meaningful |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-009](task/task-009-auth-reset-completion-back.md) | back | Implement `POST /auth/reset-password` endpoint |
| [TASK-010](task/task-010-auth-reset-completion-front.md) | front | Implement password reset form |
| [TASK-011](task/task-011-auth-reset-completion-test.md) | test | Unit tests â€” password reset service |

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

- Password policy is configured in TECH-001 via `IdentityOptions` â€” must be defined before this story is developed.
- `POST /auth/reset-password` must be exempt from the `must_reset_password` middleware (TASK-006).
