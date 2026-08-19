# TASK-024 â€” Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-008](../story-008-session-expiry.md)
**Priority:** `medium`
**Status:** `to do`

---

## Description

Configure refresh token TTL to 8 hours in token issuance service.

- Refresh token `ExpiresAt` = `IssuedAt + 8h`
- TTL must be configurable via environment variable â€” not hardcoded
- Expiry checked on every `POST /auth/refresh` call

---

## Implementation Notes

- TTL is enforced via `ExpiresAt` column in `RefreshTokens` table (TECH-002)
- Environment variable: `REFRESH_TOKEN_TTL_HOURS` â€” document in runbook
