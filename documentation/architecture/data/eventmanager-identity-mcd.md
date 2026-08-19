# EventManager Identity — Conceptual Data Model (MCD)

**Reference:** eventmanager-identity-mcd
**Status:** Validated
**Database:** EventManager_Identity
**Last updated:** 2026-08-18

---

## Purpose

This document describes the conceptual data model for the `EventManager_Identity` database.
It represents entities and their relationships independently of any implementation detail.
It is updated whenever the schema changes — revision history tracks evolution across versions.

---

## Version history

| Version | Changes |
|---|---|
| V1 | Initial schema. Introduced in V1 — no prior version. Implements ASP.NET Core Identity with project-specific extensions: `IsActive`, `MustResetPassword`, `FirstName`, `LastName`. Password history tracked for last-5-passwords policy. |

---

## Entities

### ApplicationUser

Represents an authenticated user of the system. Extends the ASP.NET Core Identity `IdentityUser`.

| Attribute | Type | Required | Description |
|---|---|---|---|
| Id | UUID | Yes | Unique identifier |
| FirstName | String (100) | Yes | First name |
| LastName | String (100) | Yes | Last name |
| Email | String (256) | Yes | Email address — unique across all users |
| NormalizedEmail | String (256) | Yes | Uppercase email — used for lookups |
| PasswordHash | String | Yes | Bcrypt password hash |
| IsActive | Boolean | Yes | Whether the account can log in |
| MustResetPassword | Boolean | Yes | Whether the user must reset password on next login |
| CreatedAt | DateTime | Yes | Account creation timestamp — UTC |

> **Standard Identity fields** (SecurityStamp, ConcurrencyStamp, PhoneNumber, etc.)
> are inherited from `IdentityUser` and not listed here — they are managed by the framework.

---

### Role

Represents a system role. Standard ASP.NET Core Identity `IdentityRole`.

| Attribute | Type | Required | Description |
|---|---|---|---|
| Id | UUID | Yes | Unique identifier |
| Name | String (50) | Yes | Role name: `organizer`, `admin`, `super_admin` |
| NormalizedName | String (50) | Yes | Uppercase role name — used for lookups |

---

### RefreshToken

Represents an active refresh token issued to a user.

| Attribute | Type | Required | Description |
|---|---|---|---|
| Id | UUID | Yes | Unique identifier |
| Token | String | Yes | Opaque token value — unique |
| ExpiresAt | DateTime | Yes | Expiry timestamp — 8h after issuance |
| IsRevoked | Boolean | Yes | Whether the token has been revoked |
| CreatedAt | DateTime | Yes | Issuance timestamp — UTC |

---

### PasswordHistory

Tracks the last 5 password hashes for each user to enforce the password reuse policy.

| Attribute | Type | Required | Description |
|---|---|---|---|
| Id | UUID | Yes | Unique identifier |
| PasswordHash | String | Yes | Bcrypt hash of a previously used password |
| CreatedAt | DateTime | Yes | Timestamp when this password was set — UTC |

---

## Relationships

| From | To | Cardinality | Description |
|---|---|---|---|
| ApplicationUser | Role | M:N | A user has one role in V1. M:N is the Identity default — constrained to one role per user in application logic |
| ApplicationUser | RefreshToken | 1:N | A user has at most one active refresh token at a time. Previous tokens are revoked on rotation |
| ApplicationUser | PasswordHistory | 1:N | A user has up to 5 password history entries. Oldest entry is removed when a new password is set beyond the 5-entry limit |

---

## Conceptual Diagram

> See [eventmanager-identity-mcd.drawio](eventmanager-identity-mcd.drawio) — open with Draw.io Integration (VS Code) or diagrams.net.

**Legend:**
- Cardinalities follow Merise notation: `(1,1) — (0,N)`, `(0,N) — (0,N)`
- The `(0,N)-(0,N)` relationship between `APPLICATION_USER` and `ROLE` is constrained to one role per user in application logic

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-18 | Document created — V1 initial identity schema |
| 1.1 | 2026-08-18 | Diagram corrected — VARCHAR replaced with NVARCHAR on FirstName, LastName, Email; NormalizedEmail row added to APPLICATION_USER entity |
