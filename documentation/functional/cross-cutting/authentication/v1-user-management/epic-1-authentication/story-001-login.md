# STORY-001 â€” Login

**Type:** Story
**Version:** V1
**Domain:** Cross-Cutting
**Scoping note:** scoping-v1-user-management-containerisation.md
**Priority:** `high`
**Status:** `to do`

---

## User Story

As a **user (organizer, admin, super admin)**,
I want **to log in with my email and password**,
so that **I can access the features corresponding to my role**.

---

## Acceptance Criteria

- [ ] Valid credentials â†’ access token (10 min) + refresh token (8h) issued as httpOnly cookies
- [ ] Valid credentials â†’ role and `mustResetPassword` returned in JSON response body
- [ ] `mustResetPassword = true` â†’ redirect to password reset screen
- [ ] Organizer â†’ redirect to event management interface
- [ ] Admin / super admin â†’ redirect to administration section
- [ ] Invalid credentials â†’ 401 with generic error message (no information leakage)
- [ ] Deactivated account â†’ 401 with specific `ACCOUNT_DEACTIVATED` error code
- [ ] Rate limit exceeded (5/min/IP or 5/min/email) â†’ 429 with `Retry-After` header

---

## Edge Cases

- [ ] Account exists but password is wrong â†’ same generic 401 as non-existent account
- [ ] Both rate limits triggered simultaneously (IP + email) â†’ single 429 response
- [ ] Login attempt on account with `mustResetPassword = true` â†’ login succeeds, redirect to reset screen (not blocked at login)
- [ ] Concurrent login attempts from same IP across different accounts â†’ IP rate limit applies globally

---

## Out of Scope

- Account creation (covered by a later story)
- Password reset flow (STORY-003)
- Session renewal (STORY-004)

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [TECH-001](tech/tech-001-identity-setup-back.md) | Tech | ASP.NET Core Identity must be configured before any authentication endpoint can be implemented |
| [TECH-002](tech/tech-002-efcore-migrations-db.md) | Tech | EF Core + Identity schema migrations must be applied before user data can be read |
| [TECH-003](tech/tech-003-superadmin-provisioning-back.md) | Tech | First super admin must be provisioned before any login can be tested end-to-end |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TECH-001](tech/tech-001-identity-setup-back.md) | back | Configure ASP.NET Core Identity |
| [TECH-002](tech/tech-002-efcore-migrations-db.md) | db | EF Core setup + initial Identity schema migrations |
| [TECH-003](tech/tech-003-superadmin-provisioning-back.md) | back | First super admin provisioning at startup |
| [TASK-001](task/task-001-auth-login-back.md) | back | Implement POST /auth/login endpoint |
| [TASK-002](task/task-002-auth-ratelimit-back.md) | back | Implement IP + email rate limiting on POST /auth/login |
| [TASK-003](task/task-003-auth-login-front.md) | front | Implement login page + form validation |
| [TASK-004](task/task-004-auth-redirect-front.md) | front | Implement role-based redirect post-login |
| [TASK-005](task/task-005-auth-login-test.md) | test | Unit tests â€” login service |

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

- The JWT access token is stored in an httpOnly cookie and is inaccessible to JavaScript. Role and `mustResetPassword` are returned in the JSON response body and stored in Pinia for the session lifetime.
- Application error codes (`ACCOUNT_DEACTIVATED`, etc.) must be defined as a shared enum before TASK-001 is started.
- Server-side logs must distinguish between "account not found" and "wrong password" for traceability, even though both return the same generic 401 to the client.
