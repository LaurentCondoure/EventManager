# STORY-004 — Silent Session Renewal

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `high`
**Status:** `to do`

---

## User Story

As an **authenticated user**,
I want **my session to renew silently when my access token expires**,
so that **my work is not interrupted without reason**.

---

## Acceptance Criteria

- [ ] 401 on expired access token → automatic `POST /auth/refresh`
- [ ] Refresh token valid → new token pair issued, original request retried transparently
- [ ] Old refresh token invalidated immediately on rotation
- [ ] Refresh token already consumed → all refresh tokens for the account revoked, redirect to login
- [ ] Refresh token expired or invalid → cookies cleared, redirect to login with session expiry message
- [ ] Concurrent requests during refresh → queued and retried after renewal completes

---

## Edge Cases

- [ ] Multiple requests expire simultaneously → only one refresh call is made, others are queued
- [ ] Refresh call fails mid-flight (network error) → queued requests receive error, user redirected to login
- [ ] Consumed refresh token presented → all tokens revoked, forced re-authentication

---

## Out of Scope

- Session expiry notification (STORY-008)
- Manual logout (STORY-005)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-001](story-001-login.md) | Story | Token issuance and httpOnly cookie setup must exist |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-012](../tasks/task-012-auth-refresh-back.md) | back | Implement `POST /auth/refresh` endpoint with token rotation and reuse detection |
| [TASK-013](../tasks/task-013-auth-refresh-front.md) | front | Implement Axios interceptor for silent renewal with request queuing |
| [TASK-014](../tasks/task-014-auth-refresh-test.md) | test | Unit tests — refresh service (rotation, reuse detection, expiry) |

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

- Consumed check and new token persistence must be atomic — database transaction required.
- Token reuse detection must revoke all refresh tokens for the account in a single bulk update.
