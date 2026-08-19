# TASK-023 â€” Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-007](../story-007-route-protection.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for RBAC enforcement and route guards.

- Unauthenticated request to protected route â†’ 401
- Authenticated request with correct role â†’ passes
- Authenticated request with insufficient role â†’ 403
- SPA navigation without session â†’ redirect to login
- SPA navigation with insufficient role â†’ redirect to role-appropriate home
