# TECH-002 — Cross-Cutting / Authentication / db

**Type:** Technical task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `db`
**Parent story:** [STORY-001](../stories/story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

> **Placement rule:** EF Core migration → attached to the first story that reads or writes to the database.

---

## Purpose

Without the Identity schema applied to the database, no user data can be read or written. This task creates the initial migrations and establishes the `EventManager_Identity` schema.

**Architectural reference:** [ADR-015](../../../architecture/adr/adr-015-identity-schema-isolation.md)

---

## Description

- Add `Microsoft.EntityFrameworkCore.SqlServer` package
- Create `IdentityDbContext` scoped to `EventManager_Identity` database
- Generate and apply initial EF Core migration covering:
  - ASP.NET Core Identity default tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.)
  - `RefreshTokens` table (`Id`, `UserId`, `Token`, `IssuedAt`, `ExpiresAt`, `ConsumedAt`)
  - `PasswordHistory` table (`Id`, `UserId`, `HashedPassword`, `CreatedAt`)
- Migrations run automatically on startup

---

## Acceptance Criteria

- [ ] `EventManager_Identity` schema is created and isolated from other domains
- [ ] All Identity default tables are present after migration
- [ ] `RefreshTokens` table is present with all required columns
- [ ] `PasswordHistory` table is present with all required columns
- [ ] Migrations apply cleanly on a fresh database at startup

---

## Implementation Notes

- `ConsumedAt` on `RefreshTokens` is required for token reuse detection (STORY-004)
- `PasswordHistory` retains the last 5 hashed passwords per user — hashing must use the same algorithm as ASP.NET Core Identity (`PBKDF2`)
- Schema must be isolated — no cross-schema foreign keys

> **ISO dev/prod rule:** Connection string must be passed via environment variable. No hardcoded values.
