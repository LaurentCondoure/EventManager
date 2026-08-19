# User Stories — V1: User Management & Containerisation

**Status:** Draft
**Based on scoping note:** V1 — User Management & Containerisation

---

## Naming convention

`US-XXX-[Domain]-[Feature]`

| Segment | Values used in V1 |
|---|---|
| Domain | `CC` (Cross-Cutting) |
| Feature | `AUTH`, `ACCOUNT`, `PASSWORD`, `PROVISIONING`, `INFRA` |

---

## Authentication (all roles)

**US-001-CC-AUTH — Login**
As any user, I want to log in with my credentials (email + password) so that I can access the features available to my role.

**Acceptance criteria:**
- A login form is accessible to any non-authenticated visitor
- A valid email + password combination grants access and redirects to the appropriate interface based on role
- An invalid combination displays an error message without specifying which field is wrong
- A deactivated account cannot log in and receives an explicit error message

---

**US-002-CC-AUTH — Logout**
As any authenticated user, I want to log out so that my session is explicitly terminated.

**Acceptance criteria:**
- A logout action is accessible from any page of the application
- After logout, the session is destroyed server-side
- The user is redirected to the login page
- Navigating back does not restore the session

---

**US-003-CC-AUTH — Session expiry**
As any authenticated user, my session automatically expires after 8 hours regardless of activity, and I am redirected to the login page so that I must re-authenticate.

**Acceptance criteria:**
- A session is composed of a short-lived access token (10 minutes) silently renewed by a refresh token valid for 8 hours
- After 8 hours, the refresh token expires and cannot be renewed; the user is redirected to the login page
- A clear message informs the user that their session has expired
- The 8-hour window starts at login and is not extended by activity

**Note:** This application is an internal tool, not a public-facing web application. The 8-hour hard expiry is a deliberate security boundary aligned with a standard workday. Idle-timeout behaviour is not required.

---

**US-004-CC-AUTH — Route protection**
As a non-authenticated visitor, I cannot access any feature of the application so that all content is protected behind authentication.

**Acceptance criteria:**
- Every route (except the login page) redirects to the login page if no valid session exists
- Direct URL access to a protected route does not reveal any content before redirection
- No API endpoint returns data to an unauthenticated request
- Every API endpoint enforces role-based access control — an authenticated user cannot access endpoints above their rank, whether via the UI or direct API call
- A request to an endpoint outside the user's role returns a 403 response, with no data exposed

---

## Super Admin — Account management

**US-005-CC-ACCOUNT — Create an admin account**
As a super admin, I want to create an admin account so that I can grant administration access to a new person.

**Acceptance criteria:**
- The creation form requires: first name, last name, email address, and a temporary password set manually by the super admin
- The temporary password must comply with the password policy: minimum 12 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character
- The email address must be unique; a duplicate triggers an explicit error
- The newly created account is active immediately
- The force reset flag is automatically set on every new account — the user will be required to change their password on first login
- The new admin can log in with the temporary password; they are immediately presented with the "You must set a new password" screen (see US-008-CC-PASSWORD)

---

**US-006-CC-ACCOUNT — Modify an admin account**
As a super admin, I want to modify an admin account so that I can keep information up to date.

**Acceptance criteria:**
- Editable fields: first name, last name, email address
- A modified email must remain unique across all accounts
- Changes are saved immediately and take effect on the next login (or current session for non-sensitive fields)
- Password is not editable via this form (handled by US-008-CC-PASSWORD)

---

**US-007-CC-ACCOUNT — Deactivate / reactivate an account**
As a user with sufficient rank, I want to deactivate or reactivate an account of strictly lower rank so that a person who has left can no longer log in, and access can be restored if needed.

**Acceptance criteria:**
- A user can only deactivate or reactivate accounts of strictly lower rank than their own
- A super admin can deactivate or reactivate any admin or organizer account
- An admin can deactivate or reactivate any organizer account, but not another admin or a super admin
- An organizer has no deactivation capability
- A deactivated account cannot log in
- Upon deactivation, the target user's refresh token is immediately revoked; access is terminated within 10 minutes at most (remaining access token lifetime), with no possibility of renewal
- A deactivated account is visually distinguishable in the account list

**Rank hierarchy:** `organizer < admin < super admin` — deactivation and reactivation are only possible strictly downward.

---

**US-008-CC-PASSWORD — Force password reset for an admin**
As a super admin, I want to force a password reset for an admin so that the user is required to set their own password after account creation or upon request.

**Acceptance criteria:**
- The super admin triggers the force reset action from the admin account detail
- The target user's refresh token is immediately revoked; access is terminated within 10 minutes at most (remaining access token lifetime), with no possibility of renewal
- On the next login attempt, the user is presented with a dedicated screen: "You must set a new password"
- The screen contains two fields: current password and new password, and a confirm button
- The new password must comply with the password policy: minimum 12 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character
- The new password cannot be identical to any of the last 5 passwords
- A user cannot reset their own password — this action is exclusively triggered by a super admin
- On confirmation, the new password is saved and the user is logged in
- The force reset flag is cleared after the new password is successfully set
- The current password field is required — the reset cannot be completed without it

**Known limitation:** This workflow does not cover the case where the current password is itself compromised. That scenario requires a separate mechanism (2FA or equivalent) and is explicitly deferred to a future version.

---

**US-009-CC-ACCOUNT — Promote an admin to super admin**
As a super admin, I want to promote an admin to super admin so that I can delegate full administration rights.

**Acceptance criteria:**
- A super admin can promote any active admin account to super admin
- The promoted account immediately gains super admin rights
- The promotion is visible in the account list
- Promotion is irreversible — a super admin cannot be demoted back to admin
- To remove a super admin's rights, the account must be deactivated (see US-007-CC-ACCOUNT)

---

## Admin — Account management

**US-010-CC-ACCOUNT — Create an organizer account**
As an admin, I want to create an organizer account so that I can grant event management access to a new organizer.

**Acceptance criteria:**
- The creation form requires: first name, last name, email address, and a temporary password set manually by the admin
- The temporary password must comply with the password policy: minimum 12 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character
- The email address must be unique; a duplicate triggers an explicit error
- The newly created account is active immediately
- The force reset flag is automatically set on every new account — the user will be required to change their password on first login
- The new organizer can log in with the temporary password; they are immediately presented with the "You must set a new password" screen (see US-012-CC-PASSWORD)

---

**US-011-CC-ACCOUNT — Modify an organizer account**
As an admin, I want to modify an organizer account so that I can keep information up to date.

**Acceptance criteria:**
- Editable fields: first name, last name, email address
- A modified email must remain unique across all accounts
- Changes take effect immediately
- Password is not editable via this form (handled by US-012-CC-PASSWORD)

---

**US-012-CC-PASSWORD — Force password reset for an organizer**
As an admin, I want to force a password reset for an organizer so that the user is required to set their own password after account creation or upon request.

**Acceptance criteria:**
- The admin triggers the force reset action from the organizer account detail
- The target user's refresh token is immediately revoked; access is terminated within 10 minutes at most (remaining access token lifetime), with no possibility of renewal
- On the next login attempt, the user is presented with a dedicated screen: "You must set a new password"
- The screen contains two fields: current password and new password, and a confirm button
- The new password must comply with the password policy: minimum 12 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character
- The new password cannot be identical to any of the last 5 passwords
- A user cannot reset their own password — this action is exclusively triggered by an admin
- On confirmation, the new password is saved and the user is logged in
- The force reset flag is cleared after the new password is successfully set
- The current password field is required — the reset cannot be completed without it

**Known limitation:** This workflow does not cover the case where the current password is itself compromised. That scenario requires a separate mechanism (2FA or equivalent) and is explicitly deferred to a future version.

---

## Administration interface — Access control

**US-013-CC-AUTH — Access to the administration section**
As an admin or super admin, I want access to a dedicated administration section so that I can manage accounts without accessing event management features.

**Acceptance criteria:**
- Admins and super admins are redirected to the administration section after login
- The administration section is not accessible to organizers
- The administration section displays the list of accounts manageable by the current role (admins see organizers; super admins see both admins and organizers)

---

**US-014-CC-AUTH — No access to event management for admins**
As an admin or super admin, I cannot access the event management features so that roles remain strictly separated.

**Acceptance criteria:**
- No event management route is accessible to an admin or super admin, whether via the UI or direct URL
- No API endpoint related to event management returns data to an admin or super admin session

---

## Organizer — Access control

**US-015-CC-AUTH — Access to event management features**
As an organizer, I want access to all existing event management features so that my current workflow is preserved without regression.

**Acceptance criteria:**
- All features available in the POC remain accessible to an organizer after V1 is deployed
- No functional regression is introduced by the addition of authentication
- An organizer is redirected to the event management interface after login

---

**US-016-CC-AUTH — No access to the administration interface for organizers**
As an organizer, I cannot access the administration interface so that account management remains reserved for admins.

**Acceptance criteria:**
- No administration route is accessible to an organizer, whether via the UI or direct URL
- No API endpoint related to account management returns data to an organizer session

---

## Super Admin — Provisioning

**US-017-CC-PROVISIONING — First super admin provisioned at startup**
As a system operator, I want the first super admin account to be provisioned automatically at application startup so that the system is usable from the first launch without manual database intervention.

**Acceptance criteria:**
- The first super admin account is created automatically when the application starts for the first time
- Credentials (email and temporary password) are configurable via environment variables
- If a super admin account already exists, the provisioning step is skipped silently
- The provisioned account behaves identically to any other super admin account

---

## ISO dev/prod

**US-018-CC-INFRA — Single-command local startup**
As a developer, I want to start the application and its database with a single command locally so that the onboarding of a new contributor requires no complex manual setup.

**Acceptance criteria:**
- A single command starts the application, the database, and any required dependencies
- The application is ready to use within a reasonable time after the command is run
- No manual configuration step is required beyond providing an environment file

---

**US-019-CC-INFRA — Identical container image in all environments**
As a developer, I want the container image used locally to be identical to the one used in production so that there is no environment-specific code path.

**Acceptance criteria:**
- A single Dockerfile produces the image used in both local and production environments
- No environment-specific code path exists in the application source
- Environment-specific values (credentials, URLs, secrets) are injected exclusively via environment variables
- The local environment reflects the production topology (same services, same networking)

---

## Open questions

| # | Question | Status |
|---|---|---|
| Q1 | ~~Does a session expire after 8h of inactivity, or 8h after creation regardless of activity?~~ | Resolved — 8h after creation, regardless of activity. See US-003-CC-AUTH. |
| Q2 | ~~How is a temporary password communicated to the new user?~~ | Resolved — force reset workflow validated. See US-008-CC-PASSWORD / US-012-CC-PASSWORD. |
| Q3 | ~~Can a super admin deactivate another super admin?~~ | Resolved — deactivation follows rank hierarchy (strictly downward). See US-007-CC-ACCOUNT. |
| Q4 | ~~Can a super admin be demoted back to admin?~~ | Resolved — rank is immutable. A super admin can only be deactivated, not demoted. See US-009-CC-ACCOUNT. |
