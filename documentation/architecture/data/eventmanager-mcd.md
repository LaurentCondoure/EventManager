# EventManager — Conceptual Data Model (MCD)

**Reference:** eventmanager-mcd
**Status:** Validated
**Database:** EventManager
**Last updated:** 2026-08-18

---

## Purpose

This document describes the conceptual data model for the `EventManager` database.
It represents entities and their relationships independently of any implementation detail.
It is updated whenever the schema changes — revision history tracks evolution across versions.

---

## Version history

| Version | Changes |
|---|---|
| V1 | Initial documentation of POC schema. `Users` entity removed — superseded by ASP.NET Core Identity in `EventManager_Identity` (see [eventmanager-identity-mcd.md](eventmanager-identity-mcd.md)). `Event` entity documented as-is from POC. |

---

## Entities

### Event

Represents a cultural event managed by an organizer.

| Attribute | Type | Required | Description |
|---|---|---|---|
| Id | UUID | Yes | Unique identifier |
| Title | String (200) | Yes | Event title |
| Description | Text | Yes | Full description |
| Date | DateTime | Yes | Event date and time |
| Location | String (200) | Yes | Location — city or venue |
| Capacity | Integer | Yes | Maximum capacity — strictly positive |
| Price | Decimal (10,2) | Yes | Entry price — zero or positive |
| Category | String (50) | Yes | Event category |
| ArtistName | String (200) | No | Artist or troupe name — optional, denormalized |
| CreatedAt | DateTime | Yes | Creation timestamp — UTC |
| UpdatedAt | DateTime | No | Last modification timestamp — UTC |

> **Note on ArtistName:** Denormalized field to avoid M:N complexity at current scale.
> Post-V1 evolution: migrate to a dedicated `Artist` entity with a relationship to `Event`
> when multi-artist or complex artist management is introduced.

---

> **Note on Users entity:** The POC defined a `Users` table as a placeholder for future
> user management. This entity is removed in V1 and superseded by `ApplicationUser`
> in the `EventManager_Identity` database, managed by ASP.NET Core Identity.

---

## Relationships

No relationships exist in V1 — the `Event` entity is standalone.
The `ArtistName` field is a denormalized attribute, not a relationship to an `Artist` entity.

Comments are stored in MongoDB (`event_comments` collection) and reference `Event.Id`
as a cross-store foreign key — this is a logical reference, not an enforced constraint.

---

## Conceptual Diagram

> See [eventmanager-mcd.drawio](eventmanager-mcd.drawio) — open with Draw.io Integration (VS Code) or diagrams.net.

**Legend:**
- `EVENT` lives in SQL Server — `EventManager` database
- `EVENT_COMMENT` lives in MongoDB — `event_comments` collection
- The dashed relationship is a logical cross-store reference, not an enforced foreign key constraint
- Cardinalities follow Merise notation: `(1,1) — (0,N)`

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-18 | Document created — V1 initial schema from POC |
