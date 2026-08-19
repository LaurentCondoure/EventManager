# STORY-008 — Session Expiry

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `medium`
**Status:** `to do`

---

## User Story

As an **authenticated user**,
I want **my session to expire after 8 hours regardless of activity**,
so that **abandoned sessions do not remain valid indefinitely**.

---

## Acceptance Criteria

- [ ] Refresh token TTL = 8 hours
- [ ] On renewal attempt after TTL elapsed → 401, cookies cleared, redirect to login
- [ ] Login page displays session expiry message on redirect from expired session

---

## Edge Cases

- [ ] User is active at hour 7h59 → next action triggers renewal → renewal fails → redirect to login with expiry message
- [ ] Multiple tabs open → expiry in one tab triggers redirect, other tabs redirect on next request

---

## Out of Scope

- Configurable session duration per role (future version)
- Idle-based session timeout (future version)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-004](story-004-silent-session-renewal.md) | Story | Expiry is surfaced through the silent renewal failure path |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-024](../tasks/task-024-auth-expiry-back.md) | back | Configure refresh token TTL to 8 hours |
| [TASK-025](../tasks/task-025-auth-expiry-front.md) | front | Implement session expiry message on login redirect |
| [TASK-026](../tasks/task-026-auth-expiry-test.md) | test | Unit tests — session expiry via renewal failure |

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

- TTL must be configurable via environment variable `REFRESH_TOKEN_TTL_HOURS` — not hardcoded.
- Expiry message must not appear on a fresh visit to the login page — only on redirect from expired session.
