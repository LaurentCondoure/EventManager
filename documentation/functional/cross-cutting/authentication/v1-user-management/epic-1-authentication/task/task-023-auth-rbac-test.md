# TASK-023 — Cross-Cutting / Authentication / test

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `test`
**Parent story:** [STORY-007](../stories/story-007-route-protection.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Write unit tests for RBAC enforcement and route guards.

- Unauthenticated request to protected route → 401
- Authenticated request with correct role → passes
- Authenticated request with insufficient role → 403
- SPA navigation without session → redirect to login
- SPA navigation with insufficient role → redirect to role-appropriate home
