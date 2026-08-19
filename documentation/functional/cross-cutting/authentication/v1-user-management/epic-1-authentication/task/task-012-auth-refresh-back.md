# TASK-012 â€” Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-004](../story-004-silent-session-renewal.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement `POST /auth/refresh` endpoint.

- Validate refresh token from httpOnly cookie against `RefreshTokens` table
- Token not found or expired â†’ 401
- Token already consumed (`ConsumedAt` is set) â†’ revoke all refresh tokens for the account â†’ 401 `TOKEN_REUSE_DETECTED`
- Token valid â†’ mark as consumed, issue new token pair, persist new refresh token, set httpOnly cookies

---

## Implementation Notes

- Consumed check and new token persistence must be atomic â€” use a database transaction
- `ConsumedAt` must be set before issuing the new token to prevent race conditions
- Revocation of all tokens on reuse must be a single bulk update
