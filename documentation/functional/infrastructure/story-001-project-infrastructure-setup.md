# STORY-001 — Project Infrastructure Setup

**Type:** Story — Technical
**Version:** V1
**Domain:** infrastructure
**Scoping note:** [scoping-v1-user-management.md](../cross-cutting/authentication/scoping-v1-user-management.md)
**Priority:** `high`
**Status:** `in progress`

---

## Objective

Set up the technical infrastructure required before any functional story can be developed or delivered. This story has no direct user value — it is a prerequisite for all other V1 stories.

> This story replaces the "As a… I want… so that…" format — it is a technical story with no end-user actor.

---

## Scope

All tasks listed below must be completed before this story is closed. No functional story can be merged into `develop` until the CI and branch protection are operational.

---

## Acceptance Criteria

- [ ] `main` and `develop` are protected — no direct commit allowed
- [ ] Only `release-*` and `hotfix-*` branches can open a PR targeting `main`
- [ ] Only `feature-*`, `docs-*`, `release-*` and `hotfix-*` branches can open a PR targeting `develop`
- [ ] CI runs on every push to `main` and every PR targeting `main` or `develop`
- [ ] A PR with failing CI cannot be merged
- [ ] A PR from an unauthorized source branch is blocked by CI

---

## Dependencies

None — this is the first story of V1.

---

## Technical Tasks

| Reference | Layer | Description |
|---|---|---|
| [TECH-001](tech-001-ci-source-branch-check-infra.md) | `infra` | Add source branch validation job to CI |

---

## Definition of Done

- [ ] All acceptance criteria are met
- [ ] All linked technical tasks are closed
- [ ] CI pipeline passes on a test PR from an authorized branch
- [ ] CI pipeline blocks a test PR from an unauthorized branch
- [ ] Branch protection rules verified on both `main` and `develop`
- [ ] No hardcoded secrets or environment-specific code paths introduced

---

## Notes

This story was created to provide a valid parent for infrastructure technical tasks that have no functional user story anchor. Any future infrastructure task with no functional parent should be attached here or to a equivalent technical setup story in the relevant version.
