# TECH-[NNN] — [Domain] / [Feature] / [Layer]

**Type:** Technical task
**Version:** [V X]
**Domain:** [Domain]
**Feature:** [Feature]
**Layer:** `back` / `front` / `test` / `infra` / `db`
**Parent story:** [STORY-NNN](story-NNN-slug.md)
**Priority:** `high` / `medium` / `low`
**Status:** `to do` / `in progress` / `done`

---

> **Placement rule:** A technical task has no direct user value. It must be attached to the
> **first user story that requires the technical constraint it addresses** — not to a later
> story that assumes it is already in place.
>
> Examples:
> - EF Core migration → attached to the first story that reads or writes to the database
> - ASP.NET Core Identity setup → attached to the first story that requires authentication
> - Docker Compose authoring → attached to the first story that requires the containerized environment
> - Varnish VCL configuration → attached to the first story that requires HTTP caching
>
> The parent story cannot be closed until this technical task is done.

---

## Purpose

[Explain why this technical task is necessary. What would break or be impossible without it?
Do not describe what to implement here — describe the architectural or infrastructure need it addresses.]

**Architectural reference:** [ADR-NNN](../../../architecture/adr/adr-NNN-slug.md)

---

## Description

[Describe precisely what needs to be done. Be specific enough that another developer could
pick this up without prior context. Include file paths, configuration keys, or schema details
where relevant.]

---

## Acceptance Criteria

- [ ] [Measurable criterion 1 — e.g. "The identity schema is applied via EF Core migration on startup"]
- [ ] [Measurable criterion 2]
- [ ] [Measurable criterion N]

---

## Implementation Notes

[Known constraints, pitfalls, or decisions to make during implementation.
Reference relevant documentation, ADRs, or external resources.]

> **ISO dev/prod rule:** This task must produce the same result locally and in production.
> No environment-specific code paths. If configuration differs between environments,
> it must be handled via environment variables documented in the runbook.

---

## Definition of Done

- [ ] All acceptance criteria are met
- [ ] ISO dev/prod verified — same behavior locally and in production
- [ ] No hardcoded secrets or environment-specific code paths
- [ ] Environment variables documented in the runbook if introduced
- [ ] Relevant ADR written or updated if a new technology is introduced
- [ ] Parent story's technical prerequisite is unblocked

---

## Notes

[Any open questions, implementation decisions, or constraints discovered during the work.]
