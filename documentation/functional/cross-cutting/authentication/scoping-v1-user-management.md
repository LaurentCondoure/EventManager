# Scoping Note — V1: User Management & Containerisation

**Status:** Validated
**Depends on:** EventManager POC functional (not yet in production)

## Objective
The EventManager POC already supports event management, but has no concept
of users: anyone can access the application.
This version lays the essential foundations for production release:
controlling who accesses the application, managing accounts securely,
and ensuring that what runs locally is identical to what runs in production.

## In scope

### User management
- Login / logout for all roles
- All features protected behind authentication

### Super Admin role
- Multiple super admins supported
- First super admin provisioned at application startup
- Create, modify, deactivate an admin account
- Force password reset for an admin
- Promote an admin to super admin

### Admin role
- Create, modify, deactivate an organizer account
- Force password reset for an organizer

### Organizer role
- Access to existing event management features
- No access to the administration interface

### Administration interface
- Dedicated section within the existing application
- Accessible to admins and super admins only
- Admins and super admins have no access to event management features

### ISO dev/prod constraint
- The local environment must be identical to the production environment
- No environment-specific behaviour or configuration differences
- The technical implementation is to be defined jointly with the Tech Lead,
  with a requirement for minimal complexity at this stage of the project

## Out of scope
- Self-registration (no user can create their own account)
- Two-factor authentication (2FA)
- Self-service password reset by the user
- Future roles (spectator, producer, venue manager)
- Anything related to other applications in the future ecosystem

## Open decisions
- None

## Acceptance criteria
- A non-authenticated visitor cannot access any feature
- A session expires after 8 hours regardless of activity; the user is redirected to the login page and must re-authenticate
- A super admin can create, modify, and deactivate an admin account
- A super admin can force a password reset for an admin
- A super admin can promote an admin to super admin
- An admin can create, modify, and deactivate an organizer account
- An admin can force a password reset for an organizer
- An authenticated organizer accesses event management features without regression
- An authenticated admin or super admin has no access to event management features
- The container image used locally is identical to the one used in production; no environment-specific code path exists in the application.
- The application and its database start with a single command locally

## Impact on existing versions
- The existing POC is modified: all its routes are now protected behind authentication
