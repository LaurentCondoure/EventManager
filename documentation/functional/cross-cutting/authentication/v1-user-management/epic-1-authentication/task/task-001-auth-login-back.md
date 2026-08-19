# TASK-001 — Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-001](../stories/story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement `POST /auth/login` endpoint.

- Validate request body (`email`, `password` — both required)
- Check account exists → 401 generic if not
- Check account is active → 401 `ACCOUNT_DEACTIVATED` if not
- Verify password via `IIdentityService` → 401 generic if invalid
- Issue access token (10 min) + refresh token (8h) as httpOnly cookies
- Persist refresh token to `RefreshTokens` table on issuance
- Return JSON body: `{ role, mustResetPassword }`

---

## Implementation Notes

- All identity operations go through `IIdentityService` — no direct dependency on `UserManager`
- Generic 401 must be identical for "account not found" and "wrong password" — no information leakage
- Server-side logs must distinguish the two cases for traceability
- Cookie configuration: `HttpOnly`, `Secure`, `SameSite=Strict`
- Application error codes must be defined as a shared enum before this task is started

**Architectural reference:** [ADR-014](../../../architecture/adr/adr-014-authentication-mechanism.md)
