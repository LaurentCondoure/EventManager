# STORY-005 — Logout

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `high`
**Status:** `to do`

---

## User Story

As an **authenticated user**,
I want **to log out**,
so that **my session is terminated securely**.

---

## Acceptance Criteria

- [ ] `POST /auth/logout` → refresh token revoked server-side
- [ ] Cookies cleared on response
- [ ] `authStore` cleared client-side
- [ ] User redirected to login page

---

## Edge Cases

- [ ] Logout called with already-expired refresh token → server still returns 200, cookies cleared
- [ ] Logout called with no cookie → 200, no error surfaced to user

---

## Out of Scope

- Logout from all devices simultaneously (future version)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-001](story-001-login.md) | Story | Token issuance and authStore must exist |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-015](../tasks/task-015-auth-logout-back.md) | back | Implement `POST /auth/logout` endpoint |
| [TASK-016](../tasks/task-016-auth-logout-front.md) | front | Implement logout action and redirect |
| [TASK-017](../tasks/task-017-auth-logout-test.md) | test | Unit tests — logout service |

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

- Revocation must happen before cookies are cleared — order of operations is critical.
- `authStore` must be cleared even if the API call fails — client-side session must always be terminated.
