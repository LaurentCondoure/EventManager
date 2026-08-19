# TASK-021 — Cross-Cutting / Authentication / back

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-007](../stories/story-007-route-protection.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Configure RBAC policies on all protected API routes.

- Define role-based authorization policies (`organizer`, `admin`, `super_admin`) via ASP.NET Core authorization
- Apply policies to all controllers and endpoints
- Unauthenticated request → 401
- Authenticated request with insufficient role → 403
- Document which roles are authorized for each endpoint

---

## Implementation Notes

- Policies must be defined centrally — not scattered across controllers
- `super_admin` inherits all `admin` permissions, `admin` inherits all `organizer` permissions — model explicitly in policy definitions
- No endpoint is left without an authorization attribute — opt-in to public, not opt-out

**Architectural reference:** [ADR-016](../../../architecture/adr/adr-016-authorisation-model.md)
