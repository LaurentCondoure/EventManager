# TASK-[NNN] — [Domain] / [Feature] / [Layer]

**Type:** Task
**Version:** [V X]
**Domain:** [Domain]
**Feature:** [Feature]
**Layer:** `back` / `front` / `test` / `infra` / `db`
**Parent story:** [STORY-NNN](story-NNN-slug.md)
**Priority:** `high` / `medium` / `low`
**Status:** `to do` / `in progress` / `done`

---

## Description

[Describe what needs to be implemented. Be specific — a task must be actionable without reading the parent story. Reference the story for context, not for understanding what to do.]

---

## Implementation Notes

[Technical details relevant to implementation: interface to use or implement, abstraction constraints, relevant ADR, known pitfalls.]

> **Abstraction rule:** No business code should depend directly on infrastructure implementations.
> If this task introduces a new technology, it must be isolated behind an interface.
> Reference the relevant ADR if one exists: [ADR-NNN](../../../architecture/adr/adr-NNN-slug.md)

---

## Definition of Done

- [ ] Implementation matches the description above
- [ ] No abstraction interface bypassed
- [ ] Error handling implemented
- [ ] Unit tests written and passing (if applicable to this layer)
- [ ] No hardcoded secrets or environment-specific code paths
- [ ] Code reviewable without assistance

---

## Notes

[Any open questions, constraints, or implementation decisions to document.]
