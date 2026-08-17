# ADR-014: Authentication mechanism

**Status:** Accepted
**Version:** V1 — User Management & Containerisation
**Date:** 10/08/2026

---

## Context

The POC has no authentication. All API routes are open. V1 requires that all routes be protected and that three roles be enforced: Super Admin, Admin, and Organizer. No new infrastructure component should be introduced for this purpose.

A token strategy must also address account deactivation: an admin must be able to deactivate an account with immediate practical effect. A fully stateless JWT approach with a long TTL is incompatible with this requirement.

Token storage must also be decided. The frontend is a Vue.js 3 SPA. The deployment topology for V1 is a single-node Docker Compose instance, with SPA and API exposed under the same base domain via a reverse proxy. CORS is already configured between the SPA and the API.

Additionally, a `must_reset_password` flag is required for provisioned accounts (ADR-017). Its enforcement mechanism must be defined.

## Options considered

**Option A — ASP.NET Core Identity + JWT (short TTL) + refresh token, tokens in httpOnly cookies**
Access token with a short TTL (10 min), emitted as an httpOnly; Secure; SameSite=Strict cookie. Refresh token with a session-duration TTL (8h), also emitted as an httpOnly; Secure; SameSite=Strict cookie, stored in the identity database and revocable. On account deactivation, the refresh token is deleted — the access token remains valid until natural expiry (max 10 min). The frontend never reads or writes tokens — the browser handles cookies automatically.
Disadvantages: requires a refresh token endpoint and database storage for refresh tokens. SameSite=Strict validity is tied to the single-domain topology — must be revisited if the topology evolves toward distinct domains.

**Option B — ASP.NET Core Identity + JWT, tokens in localStorage**
Simple implementation.
Disadvantages: tokens are accessible to JavaScript — exposed to XSS exfiltration. Rejected: the httpOnly cookie option is available without architectural overhead given the existing CORS configuration and single-domain topology.

**Option C — JWT + token whitelist (stateful)**
Every request checks the token against a whitelist (Redis). Covers both account deactivation and compromised token scenarios.
Disadvantages: introduces a synchronous read on every request; whitelist management complexity; unjustified at current scale and user population.

**Option D — Keycloak / managed cloud IdP**
Disadvantages: new infrastructure component, operational overhead not justified by V1 scope.

## Decision

Option A: ASP.NET Core Identity backed by SQL Server, with JWT access tokens (TTL 10 min) and refresh tokens (TTL 8h), both stored as httpOnly; Secure; SameSite=Strict cookies.

**Access token TTL:** 10 minutes. Silently refreshed by the client before expiry — no visible re-authentication for the user.

**Refresh token TTL:** 8 hours (working session duration). Stored in `EventManager_Identity`, one active refresh token per user.

**Refresh token rotation:** refresh tokens are rotated on every use. On each successful token refresh, a new refresh token is issued and the previous one is immediately invalidated. If a refresh request presents an already-rotated token (reuse detection), the entire session is invalidated and the user is forced to re-authenticate. This is the primary defence against refresh token theft.

**On account deactivation:** the refresh token cookie is invalidated immediately server-side. The current access token remains valid until its natural expiry (max 10 min) — this residual window is the accepted limitation.

**On logout:** the refresh token is deleted server-side and both cookies are cleared.

**`must_reset_password` enforcement:** the flag is included as a boolean claim in the JWT at token issuance. A middleware checks for this claim on every authenticated request. If present and true, the middleware returns `403` with a specific error code (`PASSWORD_RESET_REQUIRED`). The frontend intercepts this code and redirects to the password reset screen. On successful password change, the flag is cleared server-side and a fresh token pair is issued without the claim. Reading a claim from an already-parsed token is a memory operation — no additional I/O per request.

**SameSite=Strict validity:** guaranteed by the single-node Docker Compose topology with SPA and API under the same base domain.

**Varnish interaction:** Varnish must not cache any response carrying a `Set-Cookie` header. All authenticated routes pass through to the API without caching. This is standard Varnish behaviour and must be verified in the Varnish VCL configuration before V1 goes to production.

## Consequences

No new infrastructure component. Tokens are never accessible to JavaScript — XSS cannot exfiltrate credentials. Refresh tokens are persisted in the existing SQL Server identity database. Account deactivation has practical effect within 10 minutes. Refresh token rotation provides detection and containment of stolen tokens.

## Accepted limitations

- A deactivated account retains API access for up to 10 minutes (residual access token window). Consciously accepted: the window is bounded, predictable, and proportionate to the internal user population.
- SameSite=Strict is valid for the V1 single-domain topology. If the deployment topology evolves toward distinct domains, this decision must be revisited in a dedicated CTO session.
