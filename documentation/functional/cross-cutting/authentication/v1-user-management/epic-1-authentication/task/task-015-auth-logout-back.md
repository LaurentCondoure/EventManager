# TASK-015 â€” Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-005](../story-005-logout.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement `POST /auth/logout` endpoint.

- Read refresh token from httpOnly cookie
- If token exists â†’ mark as consumed in `RefreshTokens` table
- Clear httpOnly cookies (access token + refresh token)
- Return 200 regardless of token state

---

## Implementation Notes

- Revocation must happen before cookies are cleared
- Endpoint must return 200 even if no token is present â€” logout must always succeed from the user's perspective
