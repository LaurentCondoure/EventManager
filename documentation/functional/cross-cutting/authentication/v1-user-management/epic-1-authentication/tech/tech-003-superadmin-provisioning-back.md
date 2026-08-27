# TECH-003 â€” Cross-Cutting / Authentication / back

**Type:** Technical task
**Version:** V1
**Domain:** Cross-Cutting
**Feature:** Authentication
**Layer:** `back`
**Parent story:** [STORY-001](../story-001-login.md)
**Priority:** `high`
**Status:** `to do`

---

> **Placement rule:** First super admin provisioning â†’ attached to the first story that requires authentication to be testable end-to-end.

---

## Purpose

Without a super admin account in the database, no login can be tested end-to-end. This task ensures the first super admin is provisioned automatically at startup on a fresh database.

**Architectural reference:** [ADR-017](../../../../../../architecture/adr/security/adr-017-first-super-admin-provisioning.md)

---

## Description

- On application startup, check if a super admin account exists in `EventManager_Identity`
- If none exists, create one from environment variables (`SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`)
- Account is created with `MustResetPassword = true` and role `super_admin`
- If account already exists, provisioning is skipped silently
- Provisioning runs after migrations are applied

---

## Acceptance Criteria

- [ ] On fresh database startup, super admin account is created automatically
- [ ] Credentials are read exclusively from environment variables (`SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`)
- [ ] `MustResetPassword = true` is set on the provisioned account
- [ ] Role `super_admin` is assigned correctly
- [ ] Provisioning is idempotent â€” running twice does not create a duplicate or throw

---

## Implementation Notes

- Provisioning must run after migrations are applied â€” order of startup operations is critical
- Use `IHostedService` or startup filter, not a controller endpoint
- Log provisioning outcome (created / skipped) at `Information` level â€” never log the password

> **ISO dev/prod rule:** Credentials must come from environment variables. No hardcoded defaults, not even for local development.
