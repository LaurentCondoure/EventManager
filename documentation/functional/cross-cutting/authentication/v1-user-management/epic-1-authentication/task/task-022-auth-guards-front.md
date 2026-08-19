# TASK-022 — Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-007](../stories/story-007-route-protection.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement Vue Router route guards.

- Global navigation guard checks `authStore` before every route change
- No valid session → redirect to login
- Valid session but insufficient role for target route → redirect to role-appropriate home
- Route metadata defines required role(s) for each route

---

## Implementation Notes

- Guards must run after `GET /auth/me` completes on page load — session must be known before first guard evaluation
- Role check must use `authStore` exclusively — no direct cookie or token inspection in the guard
