# STORY-007 — Route Protection

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `high`
**Status:** `to do`

---

## User Story

As **any user**,
I want **unauthenticated requests to protected routes to be refused**,
so that **the application is never accessible without authentication**.

---

## Acceptance Criteria

- [ ] Every protected route returns 401 if no valid session exists
- [ ] `POST /auth/login` is the only fully public route
- [ ] Auth routes (`/auth/refresh`, `/auth/logout`, `/auth/me`, `/auth/reset-password`) accept requests with cookies but do not require a valid access token
- [ ] RBAC enforced on all protected routes — role mismatch → 403
- [ ] SPA route guards prevent navigation to protected views without a valid `authStore` session

---

## Edge Cases

- [ ] Direct URL navigation to a protected route without session → 401 from API, redirect to login from SPA
- [ ] Authenticated user navigates to a route outside their role → 403 from API, SPA guard blocks navigation

---

## Out of Scope

- Public read endpoints for the event catalogue (future version)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-001](story-001-login.md) | Story | JWT middleware and authStore must exist |
| [STORY-006](story-006-session-persistence.md) | Story | Session restoration must exist before route guards can rely on authStore |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-021](../tasks/task-021-auth-rbac-back.md) | back | Configure RBAC policies on all protected routes |
| [TASK-022](../tasks/task-022-auth-guards-front.md) | front | Implement SPA route guards |
| [TASK-023](../tasks/task-023-auth-rbac-test.md) | test | Unit tests — RBAC enforcement and route guard behavior |

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

- No endpoint is left without an authorization attribute — opt-in to public, not opt-out.
- `super_admin` inherits all `admin` permissions, `admin` inherits all `organizer` permissions — model explicitly in policy definitions.
