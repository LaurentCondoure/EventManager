# TASK-003 â€” Cross-Cutting / Authentication / front

**Type:** Task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `front`
**Parent story:** [STORY-001](../story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

## Description

Implement login page and form validation.

- Email + password fields with inline validation (both required, email format)
- On submit â†’ `POST /auth/login`
- Display generic error message on 401
- Display specific message on 401 `ACCOUNT_DEACTIVATED`
- Display 429 message with retry indication on rate limit

---

## Implementation Notes

- No error message should reveal whether the account exists
- Form must be disabled during request to prevent concurrent submissions
- Error messages sourced from a shared i18n file â€” no hardcoded strings in component
