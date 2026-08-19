# User Stories — Epic 2: Super Admin Account Management

**Version:** V1 — User Management & Containerisation
**Status:** Ready

---

## US-009 — Create admin account

**As a super admin, I can create an admin account so that I can grant access to the administration interface.**

- Fields: first name, last name, email, temporary password
- Email must be unique → 400 on duplicate
- Password policy enforced at creation
- Account created with `IsActive = true` and `must_reset_password = true`

---

## US-010 — Modify admin account

**As a super admin, I can modify an admin account (first name, last name, email) so that I can keep account information up to date.**

- Email uniqueness enforced on modification → 400 on duplicate
- Role and status are not modifiable through this flow

---

## US-011 — Deactivate admin account

**As a super admin, I can deactivate an admin account so that I can revoke access immediately when necessary.**

- Refresh token revoked immediately
- Account marked `IsActive = false`
- Target loses API access within 10 minutes (access token TTL — accepted limitation)

---

## US-012 — Reactivate admin account

**As a super admin, I can reactivate a deactivated admin account so that I can restore access when appropriate.**

- Account marked `IsActive = true`
- Target can log in again immediately

---

## US-013 — Force password reset for admin

**As a super admin, I can force a password reset for an admin account so that I can enforce a credential change when necessary.**

- Refresh token revoked immediately
- `must_reset_password = true` set on target
- Target must reset password on next login before accessing any feature

---

## US-014 — Promote admin to super admin

**As a super admin, I can promote an admin to super admin so that I can delegate full administration rights.**

- Only an active admin can be promoted
- Promotion is irreversible — deactivation is the only removal path
- Role updated immediately in the account list

---

## US-015 — View account list (super admin scope)

**As a super admin, I can view the list of accounts within my scope (admins and organizers) so that I can manage them efficiently.**

- List shows admins and organizers
- Status and role are visible for each account
