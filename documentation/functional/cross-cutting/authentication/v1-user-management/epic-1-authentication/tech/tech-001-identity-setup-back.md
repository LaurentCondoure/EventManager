# TECH-001 Cross-Cutting / Authentication / back

**Type:** Technical task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-001](../story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

> **Placement rule:** ASP.NET Core Identity setup â†’ attached to the first story that requires authentication.

---

## Purpose

Without ASP.NET Core Identity configured, no authentication endpoint can be implemented. This task establishes the identity framework that all authentication stories in V1 depend on.

**Architectural reference:** [ADR-014](../../../../../../architecture/adr/security/adr-014-authentication-mechanism.md)

---

## Description

- Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package
- Define `ApplicationUser : IdentityUser` with custom properties (`IsActive`, `MustResetPassword`)
- Configure Identity options: password policy, lockout, token providers
- Register Identity services in `Program.cs`
- Configure JWT bearer authentication middleware
- Configure httpOnly cookie handling for access and refresh tokens
- Wrap `UserManager` and `SignInManager` behind an `IIdentityService` interface

---

## Acceptance Criteria

- [ ] `ApplicationUser` is defined and extends `IdentityUser`
- [ ] Identity services are registered and resolve correctly at startup
- [ ] JWT bearer middleware is configured and validates tokens on authenticated routes
- [ ] httpOnly cookies are issued and read correctly on login
- [ ] Password policy is configured in `IdentityOptions` and enforced on all password operations
- [ ] No business code depends directly on `UserManager` or `SignInManager`

---

## Implementation Notes

- Password policy must be defined here and applied via `IdentityOptions`, this unblocks STORY-003
- `IIdentityService` is the only interface exposed to business code, no leakage of Identity internals
- Cookie configuration: `HttpOnly`, `Secure`, `SameSite=Strict`

> **ISO dev/prod rule:** JWT secret and token TTLs must be passed via environment variables. No hardcoded values.
