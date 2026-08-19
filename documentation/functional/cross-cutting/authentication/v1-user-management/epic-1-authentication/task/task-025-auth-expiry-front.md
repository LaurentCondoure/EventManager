# TASK-025 — Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-008](../stories/story-008-session-expiry.md)
**Priority:** `medium`
**Status:** `to do`

---

## Description

Implement session expiry message on login page.

- When redirect to login originates from a failed renewal → display session expiry message
- Message must be distinct from a generic unauthenticated redirect
- Implement via router query parameter or `authStore` flag set before redirect

---

## Implementation Notes

- Expiry message must not appear on a fresh visit to the login page — only on redirect from expired session
- Message sourced from i18n file — no hardcoded string in component
