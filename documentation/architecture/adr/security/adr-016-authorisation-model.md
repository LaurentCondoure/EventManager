# ADR-016: Authorisation model

**Status:** Accepted
**Version:** V1 — User Management & Containerisation
**Date:** 10/08/2026

---

## Context

V1 introduces three roles with strictly separated access perimeters: Super Admin and Admin access the administration interface only; Organizers access event management features only. No role has access to both perimeters. All API routes must be protected.

## Options considered

**Option A — ASP.NET Core policy-based authorisation with claims**
Roles stored as JWT claims. Named policies declared at startup (`Program.cs`), applied via `[Authorize(Policy = "...")]` attributes. Standard .NET approach, auditable, zero additional dependency.

**Option B — Custom middleware**
Bespoke role-checking logic per route. Disadvantages: reinvents the framework; harder to audit; risk of inconsistent enforcement across controllers.

**Option C — External policy engine (OPA)**
Unjustified at this role count and scale.

## Decision

ASP.NET Core claims-based identity with named policy-based authorisation. Role is a claim in the JWT. Policies are declared at startup and applied declaratively via attributes.

## Consequences

Consistent, auditable enforcement across all controllers. Policy changes require redeployment — acceptable for a static role set.

## Accepted limitations

No resource-level authorisation (e.g. an organizer restricted to their own events). Not required by the V1 scoping note.
