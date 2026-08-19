# TASK-021 â€” Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-007](../story-007-route-protection.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Configure RBAC policies on all protected API routes.

- Define role-based authorization policies (`organizer`, `admin`, `super_admin`) via ASP.NET Core authorization
- Apply policies to all controllers and endpoints
- Unauthenticated request â†’ 401
- Authenticated request with insufficient role â†’ 403
- Document which roles are authorized for each endpoint

---

## Implementation Notes

- Policies must be defined centrally â€” not scattered across controllers
- `super_admin` inherits all `admin` permissions, `admin` inherits all `organizer` permissions â€” model explicitly in policy definitions
- No endpoint is left without an authorization attribute â€” opt-in to public, not opt-out

**Architectural reference:** [ADR-016](../../../../../../architecture/adr/security/adr-016-authorisation-model.md)
