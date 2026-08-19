# User Stories — Epic 3: Admin Account Management

**Version:** V1 — User Management & Containerisation
**Status:** Ready

---

## US-016 — Create organizer account

**As an admin, I can create an organizer account so that I can grant access to event management.**

- Fields: first name, last name, email, temporary password
- Email must be unique → 400 on duplicate
- Password policy enforced at creation
- Account created with `IsActive = true` and `must_reset_password = true`

---

## US-017 — Modify organizer account

**As an admin, I can modify an organizer account (first name, last name, email) so that I can keep account information up to date.**

- Email uniqueness enforced on modification → 400 on duplicate
- Role and status are not modifiable through this flow

---

## US-018 — Deactivate organizer account

**As an admin, I can deactivate an organizer account so that I can revoke access immediately when necessary.**

- Refresh token revoked immediately
- Account marked `IsActive = false`
- Target loses API access within 10 minutes (access token TTL — accepted limitation)

---

## US-019 — Reactivate organizer account

**As an admin, I can reactivate a deactivated organizer account so that I can restore access when appropriate.**

- Account marked `IsActive = true`
- Target can log in again immediately

---

## US-020 — Force password reset for organizer

**As an admin, I can force a password reset for an organizer account so that I can enforce a credential change when necessary.**

- Refresh token revoked immediately
- `must_reset_password = true` set on target
- Target must reset password on next login before accessing any feature

---

## US-021 — View account list (admin scope)

**As an admin, I can view the list of accounts within my scope (organizers only) so that I can manage them efficiently.**

- List shows organizers only — no admin or super admin accounts visible
