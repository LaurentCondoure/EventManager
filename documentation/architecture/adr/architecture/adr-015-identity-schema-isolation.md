# ADR-015: Identity schema isolation

**Status:** Accepted
**Version:** V1 — User Management & Containerisation
**Date:** 10/08/2026

---

## Context

ASP.NET Core Identity requires a SQL Server persistence store. The system already runs a SQL Server instance for event data (managed by EF Core — see ADR-018). The placement of identity tables relative to event data must be decided.

## Options considered

**Option A — Same database as events (EventManager)**
Zero additional configuration.
Disadvantages: identity and event schema share the same migration surface; a schema change on one side risks the other.

**Option B — Separate database on the same SQL Server instance**
Identity in `EventManager_Identity`, events in `EventManager`. Logical isolation, no new container, minimal configuration overhead.

**Option C — Separate SQL Server container**
Full physical isolation.
Disadvantages: additional container, additional resource consumption, unjustified at current scale.

## Decision

Two distinct databases on the same SQL Server container: `EventManager` (events) and `EventManager_Identity` (identity). Each has its own EF Core `DbContext`, its own connection string, and an independent migration surface.

## Consequences

Identity and event schema migrations are independent. Operational cost is zero — same container, two connection strings, two `DbContext` instances managed by EF Core.

## Accepted limitations

Both databases share the same SQL Server container. A container-level failure affects both simultaneously. Acceptable at current scale.
