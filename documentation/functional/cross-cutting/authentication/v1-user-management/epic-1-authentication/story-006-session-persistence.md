# STORY-006 â€” Session Persistence on Page Refresh

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `high`
**Status:** `to do`

---

## User Story

As an **authenticated user**,
I want **to remain authenticated when I refresh my browser window**,
so that **I do not lose my session due to a normal navigation action**.

---

## Acceptance Criteria

- [ ] On every page load or refresh â†’ SPA calls `GET /auth/me` automatically
- [ ] Valid session â†’ role and `mustResetPassword` restored in `authStore`, user lands on appropriate interface
- [ ] `mustResetPassword = true` â†’ redirect to reset screen, not to login
- [ ] No valid session â†’ redirect to login
- [ ] Rate limit exceeded (3/min/user) â†’ 429 with `Retry-After` header, no redirect to login
- [ ] If access token expired but refresh token valid â†’ server rotates tokens silently and returns 200

---

## Edge Cases

- [ ] Page refresh during password reset flow â†’ `mustResetPassword = true` restored, user stays on reset screen
- [ ] 429 on `/auth/me` â†’ user sees error, session state unchanged, no redirect
- [ ] Access token expired + refresh token expired â†’ 401 â†’ redirect to login

---

## Out of Scope

- Session renewal triggered by user actions (STORY-004)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-001](story-001-login.md) | Story | authStore and token issuance must exist |
| [STORY-002](story-002-forced-password-reset-gate.md) | Story | `mustResetPassword` handling must exist before restoration can redirect correctly |
| [STORY-004](story-004-silent-session-renewal.md) | Story | Token rotation on `/auth/me` reuses the same rotation logic |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-018](task/task-018-auth-me-back.md) | back | Implement `GET /auth/me` endpoint |
| [TASK-019](task/task-019-auth-me-front.md) | front | Implement session restoration on page load |
| [TASK-020](task/task-020-auth-me-test.md) | test | Unit tests â€” `/auth/me` endpoint and session restoration |

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

- `GET /auth/me` must be called before any route guard runs â€” app must not render until session is known.
- Token rotation on this endpoint must reuse `ITokenService` â€” no duplicated rotation logic.
- 429 must not redirect to login â€” display error only.
