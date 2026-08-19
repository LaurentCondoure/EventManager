# TASK-009 â€” Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-003](../story-003-password-reset-completion.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement `POST /auth/reset-password` endpoint.

- Verify current password via `IIdentityService` â†’ 400 if incorrect
- Validate new password against policy (configured in TECH-001) â†’ 400 with policy error if not compliant
- Check new password against last 5 in `PasswordHistory` â†’ 400 `PASSWORD_REUSE` if matched
- Save new password, add to `PasswordHistory`, clear `MustResetPassword` flag
- Issue fresh token pair as httpOnly cookies
- Return role in response body for redirect

---

## Implementation Notes

- `PasswordHistory` must retain only the last 5 entries per user â€” prune on write
- Hashing must use the same algorithm as ASP.NET Core Identity (`PBKDF2`)
- Endpoint must be accessible with a token carrying `must_reset_password = true` â€” exempt from TASK-006 middleware
- All operations (password save, history update, flag clear, token issue) must be atomic
