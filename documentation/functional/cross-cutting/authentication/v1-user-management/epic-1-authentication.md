# User Stories — Epic 1: Authentication

**Version:** V1 — User Management & Containerisation
**Status:** Splitted

---

## US-001 — Login

**As any user, I can log in with my email and password so that I can access the features corresponding to my role.**

- Valid credentials → token pair issued, role-based redirect (organizer → event management, admin/super admin → administration section)
- Invalid credentials → generic error message (no information leakage)
- Deactivated account → specific message, access refused
- Rate limit: 5 requests per minute per IP and 5 per minute per email → 429 with Retry-After header if either limit is exceeded

---

## US-002 — Forced password reset gate

**As any authenticated user, I am automatically redirected to the password reset screen when my account has a forced reset pending, so that I cannot access any other feature until I have changed my password.**

- `must_reset_password` flag present → 403 PASSWORD_RESET_REQUIRED → redirect to reset screen regardless of role
- No other route is accessible until the reset is completed

---

## US-003 — Password reset completion

**As any user, I can reset my password when required, so that I can recover access to my account.**

- Current password must be provided and verified
- New password must comply with the password policy
- New password must not be one of the last 5 passwords
- On success → flag cleared, fresh token pair issued, redirect to role-appropriate interface

---

## US-004 — Silent session renewal

**As any authenticated user, my session renews silently when my access token expires, so that my work is not interrupted without reason.**

- 401 on expired access token → automatic POST /auth/refresh
- Refresh token valid → new token pair issued, original request retried transparently
- Refresh token expired or invalid → cookies cleared, redirect to login with session expiry message
- On each renewal, the consumed refresh token is immediately invalidated and replaced by a new one (token rotation)
- If a refresh token is presented that has already been consumed, all refresh tokens for the account are revoked immediately and the user is forced to re-authenticate (token reuse detection)

---

## US-005 — Logout

**As any authenticated user, I can log out so that my session is terminated securely.**

- Refresh token revoked server-side
- Cookies cleared, Pinia state cleared, redirect to login page

---

## US-006 — Session persistence on page refresh

**As any authenticated user, I remain authenticated when I refresh my browser window, so that I do not lose my session due to a normal navigation action.**

- On every page load or refresh, the SPA calls GET /auth/me automatically
- If a valid session exists → role and `mustResetPassword` restored, user lands on the appropriate interface without being redirected to login
- If `mustResetPassword` is true → user is redirected to the reset screen, not to login
- If no valid session exists → user is redirected to login
- Rate limit: 3 requests per minute per user → 429 with Retry-After header if exceeded, no redirect to login

---

## US-007 — Route protection

**As any user, a non-authenticated request to any protected route is refused, so that the application is never accessible without authentication.**

- Every route returns 401 if no valid session exists
- No public route exists other than POST /auth/login

---

## US-008 — Session expiry

**As any authenticated user, my session expires after 8 hours regardless of activity, so that abandoned sessions do not remain valid indefinitely.**

- Refresh token TTL = 8h
- On expiry → renewal fails → redirect to login with session expiry message
