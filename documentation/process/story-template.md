# STORY-[NNN] — [Title]

**Type:** Story
**Version:** [V X]
**Domain:** [Domain]
**Scoping note:** [scoping-vX-name.md](../scoping-vX-name.md)
**Priority:** `high` / `medium` / `low`
**Status:** `to do` / `in progress` / `done`

---

## User Story

As a **[role]**,
I want **[action]**,
so that **[outcome]**.

---

## Acceptance Criteria

- [ ] [Criterion 1]
- [ ] [Criterion 2]
- [ ] [Criterion N]

---

## Edge Cases

- [ ] [Edge case 1]
- [ ] [Edge case 2]

---

## Out of Scope

- [What this story explicitly does not cover]

---

## Dependencies

| Depends on | Type | Reason |
|---|---|---|
| [STORY-NNN or TECH-NNN] | Story / Tech | [Why this story is blocked until the dependency is done] |

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TASK-NNN](task-NNN-domain-feature-layer.md) | [Layer] | [Short description] |
| [TASK-NNN](task-NNN-domain-feature-layer.md) | [Layer] | [Short description] |

> **Note:** Technical infrastructure tasks with no direct user value (e.g. EF Core migration,
> ASP.NET Core Identity setup, Docker Compose authoring) must be attached to the first story
> that requires them — not to a later story that assumes they are already in place.
> A story cannot be closed until all its technical tasks are done.

---

## Definition of Done

- [ ] All acceptance criteria are met
- [ ] All edge cases are handled
- [ ] All linked tasks are closed
- [ ] No regression on existing features
- [ ] Tests written and passing
- [ ] Error handling implemented on all paths
- [ ] API documentation updated if an endpoint was added or modified
- [ ] No scope creep introduced

---

## Notes

[Any additional context, open questions, or design decisions relevant to this story.]
