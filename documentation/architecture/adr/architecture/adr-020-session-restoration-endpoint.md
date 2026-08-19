# ADR-020: Session restoration endpoint — GET /auth/me

**Reference:** adr-020-session-restoration-endpoint
**Status:** Accepted
**Version introduced:** V1
**Last updated:** 2026-08-19

**Related ADRs:**
- ADR-014 — Authentication mechanism (httpOnly cookie model, token TTLs)
- ADR-019 — Rate limiting on authentication endpoints (`auth-me-user` policy)

**Related design documents:**
- design-authentication.md — SD-5.4 (canonical flow reference)
- design-account-management.md — interaction overview (force reset, reactivation flows)

---

## Context

The JWT access token is stored in an httpOnly cookie — it is inaccessible to JavaScript.
On login, the API returns the role and `mustResetPassword` flag in the JSON response body,
which the SPA stores in Pinia. This works for the initial session, but Pinia state is lost
on page refresh. Without a dedicated endpoint to restore session context, every page refresh
would require a full re-authentication.

This gap was identified during the design phase and addressed in `design-authentication.md`
via a new endpoint: `GET /auth/me`.

---

## Endpoint behaviour

- Validates the access token from the httpOnly cookie
- If the access token is expired but the refresh token is valid, performs a silent token
  rotation (new token pair issued, old refresh token invalidated, `Set-Cookie` headers
  returned)
- Returns `{ role, mustResetPassword }` in the JSON response body on success
- Returns `401 Unauthorized` if neither token is valid — SPA redirects to login
- Rate limited: 3 requests/min per authenticated user (ADR-019, policy `auth-me-user`)

---

## Options considered

**Option A — GET /auth/me (dedicated session restoration endpoint)**
Single endpoint, called once per page load. Returns role and flags from a valid access token
without side effects when the token is still valid. Performs silent renewal only when
necessary. Rate limited. Clean separation of concern from login and from the refresh flow.

**Option B — Reuse POST /auth/refresh for session restoration**
The SPA calls `POST /auth/refresh` unconditionally on every page load. Since the access
token is in an httpOnly cookie and is inaccessible to JavaScript, the SPA cannot determine
whether it is still valid — it must call refresh regardless. This means every page load
generates a refresh token rotation: the old refresh token is invalidated, a new one is
written to the Identity Store, and new cookies are issued.

This conflates two distinct operations (session restoration and token renewal), generates
unnecessary Identity Store writes on every page load for the entire session lifetime, and
wastes refresh token TTL. Rejected on design correctness grounds — at V1 scale the load
is negligible, but the conflation is architecturally unsound.

**Option C — Store role in a readable (non-httpOnly) cookie**
Avoid the need for a restoration endpoint by making the role accessible to JavaScript.
Disadvantages: exposes session state to XSS. Inconsistent with the httpOnly cookie security
model established in ADR-014. Rejected.

---

## Decision

Option A: dedicated `GET /auth/me` endpoint. Added to the authentication surface of the API.
The canonical flow is defined in `design-authentication.md` SD-5.4.

**Architectural note — GET with side effects:**
`GET /auth/me` may issue `Set-Cookie` headers when token rotation occurs. This is an
intentional exception to the HTTP convention that GET requests are safe and idempotent.
The mutation (token rotation) is a security mechanism transparent to the caller, not a
data mutation. Two consequences:

- **Varnish must never cache this endpoint's response** — enforced automatically when
  `Set-Cookie` is present, but must be explicitly enforced in VCL for non-rotation
  responses where `Set-Cookie` is absent.
- **Rate limit implementation must account for this:** a successful `200` with `Set-Cookie`
  must still count against the per-user limit, not be treated as a passthrough.

---

## Consequences

- One new endpoint on the authentication surface of the API
- VCL must explicitly pass through `GET /auth/me` regardless of response shape —
  to be verified during implementation
- Rate limiting policy `auth-me-user` applies (ADR-019)
- No additional NuGet package required — rate limiting is handled by
  `Microsoft.AspNetCore.RateLimiting` (see ADR-019)

---

## Accepted limitations

None beyond those inherited from ADR-014:
- Residual access window on deactivation (up to 10 min)
- SameSite=Strict valid for single-domain topology only

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-19 | Document created — V1 session restoration endpoint |
