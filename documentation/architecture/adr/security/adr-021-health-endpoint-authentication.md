# ADR-021: Authentication on the health endpoint — JWT or a static system API key

**Reference:** adr-021-health-endpoint-authentication
**Status:** Accepted
**Version introduced:** V1
**Last updated:** 2026-08-20

**Related ADRs:**
- [ADR-014 — Authentication mechanism](adr-014-authentication-mechanism.md)
- [ADR-016 — Authorisation model](adr-016-authorisation-model.md)

---

## Context

`GET /health` was introduced pre-V1 as an unauthenticated liveness endpoint, following the
common REST convention that health checks are public. That convention was inherited by
default, not chosen deliberately — checking the actual project state:

- No container orchestrator or load balancer currently polls it: the API is not yet
  containerised with a wired-up healthcheck (ADR-013 covers containerisation strategy, but no
  `api` service or `HEALTHCHECK` exists in `docker-compose.yml` as of V1).
- It is not called by the frontend.
- It is referenced only in documentation (`documentation/architecture/flows/GET-health.md`,
  `documentation/architecture/overview.md`, `README.md`) and in two local convenience scripts
  (`scripts/Deploy-Local.ps1`, `scripts/Setup-IIS.ps1`) that print its URL for a human to open
  after deploying.

In practice, `/health` is used two ways, neither of which is "anonymous caller":

- **Manually, by staff** — an admin or super admin hitting it to visually confirm the API is
  up, including during issue investigation.
- **By a future automated probe** — not concretely defined yet, but expected to be a trusted
  system caller, not an anonymous one.

TECH-001 (ASP.NET Core Identity setup) needed a genuinely protected route to prove the JWT
Bearer pipeline end-to-end — there was no other one yet, since RBAC (TASK-021, the task, not
this ADR) and the rest of the authenticated surface don't exist yet. `/health` became that
first protected route, which is what prompted this decision.

## Options considered

**Option A — Keep it public/unauthenticated**
Matches the general REST liveness-probe convention. Rejected: nothing currently polls it
anonymously, and it would sit as unauthenticated attack surface — including whatever
diagnostic detail it may grow to report — for a caller that doesn't exist.

**Option B — Require authentication, JWT only (any authenticated role)**
Covers the staff/investigation use case cleanly. Rejected as the sole mechanism: a future
automated probe would have to run the full human login/cookie flow (short-lived access token,
refresh rotation, reuse detection, `SameSite=Strict`) just to poll a status endpoint. That
machinery exists to defend a browser session against hijacking — it is unnecessary ceremony
for a trusted system caller, and there is no `system` role in ADR-016's model to hang such an
account off without extending that ADR.

**Option C — Full API key management (DB-backed keys, issue/revoke/rotate per integration, audit trail)**
Rejected for now: no concrete external caller exists yet, so the actual requirements (one key
vs. several, rotation policy, scoping) aren't known. Building the full lifecycle — schema,
migration, admin CRUD endpoints, hashed storage, audit log, frontend admin UI — would be
guessing at requirements nobody has stated. Same reasoning as ADR-012 (`IDbConnectionFactory`
removal): an abstraction with no current concrete need adds cost without benefit, and can be
introduced when a real caller and its requirements exist.

**Option D — Require authentication, either JWT or a single static API key (chosen)**
A minimal, env-var-sourced key (`ApiKey:Value`, same ISO dev/prod rule as `Jwt:Secret` — no
hardcoded value), checked by a small second `AuthenticationHandler` (~50 lines) registered
alongside JWT Bearer. `/health` accepts either scheme. No database, no CRUD, no rotation UI —
sized to match that the external caller is still conceptual, while giving it a mechanism that
doesn't force it through session machinery meant for browsers.

## Decision

Option D. `GET /health` requires authentication via either:
- A valid JWT access token in the httpOnly cookie (ADR-014) — any authenticated role, no RBAC
  restriction yet (see Accepted limitations); or
- The static system key in the `X-Api-Key` header, compared in constant time against
  `ApiKey:Value`.

Implemented as a second authentication scheme (`ApiKeyAuthenticationDefaults.AuthenticationScheme`),
not the default — routes opt in explicitly via
`RequireAuthorization(policy => policy.AddAuthenticationSchemes(Jwt, ApiKey).RequireAuthenticatedUser())`.
Multi-scheme evaluation is OR: authentication succeeds if either scheme succeeds.

## Consequences

- `/health` can no longer be polled anonymously. `scripts/Deploy-Local.ps1`,
  `scripts/Setup-IIS.ps1`, and `documentation/architecture/flows/GET-health.md` describe it as
  a public check — they need updating to reflect that a token or the system key is now
  required. Tracked as follow-up documentation work, not blocking this ADR.
- No current operational impact: no orchestrator or load balancer polls this endpoint today.
- Establishes the pattern for any future genuinely-system-facing endpoint: a second scheme
  alongside JWT Bearer, opted into per-route, rather than a blanket exception to
  authentication.

## A known, separate gap — not addressed by this ADR

While reviewing `/health`, `GET /api/events/categories` (and the rest of the current
`/api/events/*` surface) was identified as fully anonymous today, which conflicts with the V1
requirement in `scoping-v1-user-management.md`: *"A non-authenticated visitor cannot access
any feature."* This is real, but it is a different problem from `/health`'s: it is a business
data endpoint consumed by the frontend SPA for every authenticated user (organizer, admin,
super admin) — not a system/ops endpoint — so it needs plain JWT protection (`RequireAuthorization()`,
no API key scheme involved) once a caller can actually obtain a token.

It is deliberately **not** addressed here because:
- No RBAC policies exist yet, and no login endpoint exists yet (TECH-002, TECH-003, TASK-001
  are all still pending) — there is no way to obtain a token to call it with.
- The frontend currently calls it anonymously; locking it now would break the running app
  ahead of the corresponding frontend/token work, with no way to verify the fix.
- TASK-021 (STORY-007, "Route protection") is the ticket already scoped for exactly this:
  *"Configure RBAC policies on all protected API routes... No endpoint is left without an
  authorization attribute — opt-in to public, not opt-out."* Securing routes piecemeal outside
  that ticket risks the inconsistent, scattered enforcement ADR-016 already rejected Option B
  (custom middleware) over.

Flagged here so the gap is documented rather than silently found and dropped. TASK-021 is
where it gets closed.

## Accepted limitations

- No RBAC restriction on the JWT path — any authenticated role can call `/health`, not just
  admin/super admin. Acceptable until RBAC policies exist (TASK-021); revisit then if
  staff-only access is actually required.
- The static API key is a single shared secret with no per-caller identity, rotation, or
  revocation. Acceptable while the caller is conceptual; if a concrete external system
  arrives with real requirements (multiple integrations, rotation policy, audit needs), that
  is a new ADR and ticket for managed API keys (Option C above), not a retrofit of this one.
- If ADR-013's containerisation work later needs a genuinely public liveness endpoint (e.g.
  for a Docker `HEALTHCHECK` or an external load balancer that cannot hold a credential), that
  is a new, separate endpoint to introduce deliberately — not a reason to reopen `/health`.

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-20 | Document created — `/health` requires JWT or static system API key |
