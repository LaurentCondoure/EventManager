# Release Management Guidelines

**Reference:** GUIDELINES-RELEASE-001
**Status:** Validated
**Date:** 2026-08-17

---

## Purpose

This document defines the branching strategy, release process, and release gates for the EventManager project. It applies to all versions and is reviewed at the start of each version if the delivery context changes.

---

## 1. Branching Strategy

### 1.1 Branch model

```
main
  └── develop
        ├── feature-[slug]
        │     └── [optional] subtask-[slug]
        └── release-vX
              └── stabilisation bug fixes
                    ├── merge into main (tag vX.Y.Z)
                    └── merge back into develop
```

### 1.2 Branch definitions

| Branch | Purpose | Created from | Merged into |
|---|---|---|---|
| `main` | Stable production code. Every commit on `main` represents a released version. | — | — |
| `develop` | Integration branch. Accumulates completed features for the current version. | `main` | — |
| `feature-[slug]` | Implements a user story or a group of related tasks. | `develop` | `develop` |
| `subtask-[slug]` | Implements a single task or technical task within a feature. | `feature-[slug]` | `feature-[slug]` |
| `release-vX` | Stabilisation branch for the version. Isolates release preparation from ongoing development. | `develop` | `main` + `develop` |
| `hotfix-[slug]` | Addresses a critical defect in production that cannot wait for the next version. | `main` | `main` + `develop` |
| `docs-[slug]` | Adds or updates transversal documentation (TAD, ADR, design, guidelines, runbook, changelog). | `develop` | `develop` |

### 1.3 Naming conventions

| Branch | Pattern | Example |
|---|---|---|
| Feature | `feature-[slug]` | `feature-superadmin-login` |
| Subtask | `subtask-[slug]` | `subtask-identity-setup` |
| Release | `release-vX` | `release-v1` |
| Hotfix | `hotfix-[slug]` | `hotfix-login-redirect` |
| Documentation | `docs-[slug]` | `docs-tat-v1` |

### 1.4 Rules

- `main` is protected — no direct commit. Only merge from `release-vX` or `hotfix-*`.
- `develop` is protected — no direct commit. Only merge from `feature-*` branches via pull request.
- A `feature-[slug]` branch maps to one user story. If a story requires subtasks, each subtask gets its own branch created from the feature branch.
- A `release-vX` branch is created from `develop` when development is complete for the version. Only stabilisation bug fixes are committed on this branch — no new features.
- After merge into `main`, `release-vX` must be merged back into `develop` to keep branches in sync.
- `release-vX` is deleted after both merges are done.
- A `feature-[slug]` branch is deleted after merge into `develop`.
- A `subtask-[slug]` branch is deleted after merge into its parent `feature-[slug]`.
- Branch names are in kebab-case.
- A `docs-[slug]` branch is used for transversal documentation that does not belong to a specific feature (TAD, ADR, design, guidelines, runbook, changelog). Feature-specific documentation (scoping note, DoD, stories, tasks) is committed on the corresponding `feature-[slug]` branch.
- A `docs-[slug]` branch is deleted after merge into `develop`.

### 1.5 Version lifecycle on branches

```
[V1 development]
  develop <── feature-superadmin-login
                  └── subtask-identity-setup
                  └── subtask-ef-core-migration

[V1 stabilisation]
  develop ──► release-v1
                  └── bug-001-login-redirect (fix)
                  └── changelog-v1.md committed

[V1 release]
  release-v1 ──► main    (tag: v1.0.0)
  release-v1 ──► develop (sync)
  release-v1   ──► deleted
```

---

## 2. Release Process

### 2.1 Overview

```
Development complete on develop
  └── release-vX created from develop
        └── Stabilisation (bug fixes, changelog)
              └── Release gates verified (Section 3)
                    └── release-vX merged into main (tag vX.Y.Z)
                          └── release-vX merged back into develop
                                └── Deployment to production
                                      └── Version declared stable
```

### 2.2 Step-by-step procedure

**Step 1 — Create the release branch**

```bash
git checkout develop
git checkout -b release-vX
git push origin release-vX
```

From this point, no new feature is merged into `release-vX`. Only stabilisation bug fixes and the changelog are committed here.

**Step 2 — Stabilise**

Fix any blocking bugs discovered during validation directly on `release-vX`. Document each fix as a `bug-[NNN]-[slug].md` file in the relevant functional domain subfolder.

**Step 3 — Prepare the changelog**

Write or finalize `documentation/changelog/changelog-vX.md` and commit it to `release-vX`.

**Step 4 — Verify release gates**

All release gates defined in Section 3 must be satisfied before proceeding. Do not proceed if any gate fails.

**Step 5 — Merge into main**

```bash
git checkout main
git merge --no-ff release-vX -m "release: vX.Y.Z"
git push origin main
```

The `--no-ff` flag preserves the merge commit and makes the release boundary explicit in the history.

**Step 6 — Tag the release**

```bash
git tag -a vX.Y.Z -m "Release vX.Y.Z — [Version name]"
git push origin main --tags
```

Versioning follows semantic versioning (semver):
- `X` — major version (breaking change or major functional increment)
- `Y` — minor version (new features, no breaking change)
- `Z` — patch version (bug fix or hotfix)

**Step 7 — Merge back into develop**

```bash
git checkout develop
git merge --no-ff release-vX -m "chore: sync release-vX back into develop"
git push origin develop
```

**Step 8 — Delete the release branch**

```bash
git branch -d release-vX
git push origin --delete release-vX
```

**Step 9 — Deploy to production**

Follow the deployment procedure defined in `documentation/runbooks/runbook-vX.md`.

**Step 10 — Verify production**

Validate that the deployment is successful using the verification steps defined in the runbook. If verification fails, execute the rollback procedure immediately.

**Step 11 — Declare the version stable**

Once production is verified, update the TAD and the scoping note status to `Validated`. The version is now stable and the next version scoping can begin.

### 2.3 Hotfix process

A hotfix addresses a critical defect discovered in production that cannot wait for the next version.

```
main ──► hotfix-[slug]
              └── fix committed
                    ├── merged into main (tag: vX.Y.Z+1)
                    └── merged back into develop
```

**Rules:**
- A hotfix branch is created from `main`, not from `develop`.
- After merge into `main`, the hotfix must be merged back into `develop` to keep branches in sync.
- A hotfix is documented as a `bug-[NNN]-[slug].md` file in the relevant functional domain subfolder.
- The hotfix produces a patch release (`Z` increment in semver).

---

## 3. Release Gates

A release is authorized only when all gates below are satisfied. Gates are evaluated in order — a failure at any gate blocks the release.

### 3.1 Automated gates (CI)

These gates are enforced automatically on every push and pull request to `main`. A failed CI job blocks the merge.

| Gate | Threshold | Enforced by |
|---|---|---|
| Test coverage | ≥ 96% | CI — coverage job |
| All tests passing | 100% green | CI — test job |
| Documentation link integrity | 0 broken links | CI — `Check-DocLinks.ps1` |
| Build | Successful | CI — build job |

### 3.2 Manual gates (human validation)

These gates require explicit human sign-off before the release merge is authorized.

| Gate | Validated by | Artifact |
|---|---|---|
| All version DoD criteria met | PM + Tech Lead | `dod-vX-[name].md` fully checked |
| All stories in scope closed | PM | All `story-[NNN]-*.md` in the version subfolder marked done |
| All tasks and technical tasks closed | Tech Lead | All `task-*` and `tech-*` files in the version subfolder marked done |
| No open bugs blocking release | Tech Lead | No `bug-[NNN]-*.md` with status `to do` or `in progress` |
| TAD updated and validated | CTO | `tat-eventmanager.md` version section complete |
| Runbook written and validated | Tech Lead | `runbook-vX.md` complete |
| Changelog written | Tech Lead / PM | `changelog-vX.md` complete |

### 3.3 Security gate

> **Status: Deferred**
> Security gates will be defined and added to this section once the authentication ADR is validated.
> They will be integrated as part of the CI pipeline at the end of the version that introduces authentication.

### 3.4 Release authorization

```
All automated gates green   : ☐
All manual gates validated   : ☐
Security gate                : ☐ Deferred to end of version

Release authorized           : ☐
Authorized by                : _______________  Date: _______________
```

---

## 4. CI Pipeline — Current State

The CI pipeline runs on every push and pull request to `main` via GitHub Actions (`.github/workflows/ci.yml`).

| Job | Trigger | Description |
|---|---|---|
| `build` | push / PR to `main` | Builds the application |
| `test` | push / PR to `main` | Runs all tests and measures coverage |
| `docs-link-check` | push / PR to `main` | Validates relative links in `documentation/` |

CD (Continuous Deployment) covers the local environment only at this stage. Production deployment is manual, following the runbook procedure.

> A production CD pipeline will be scoped in a future version once the deployment environment is finalized.

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-17 | Document created |
| 1.1 | 2026-08-17 | Added release branch to branching strategy and release process |
| 1.2 | 2026-08-17 | Added docs branch convention. Added GitHub repository configuration section. |

---

## 5. GitHub Repository Configuration

### 5.1 Branch protection rules

Both `main` and `develop` are protected branches. The following rules apply to both.

| Rule | Value |
|---|---|
| Require pull request before merging | ✅ |
| Required reviewers | 0 — solo project, status checks are the real gate |
| Require status checks to pass | ✅ — `build`, `test`, `docs-link-check` |
| Do not allow bypassing | ✅ |

> No direct commit is permitted on `main` or `develop` under any circumstance, including for the sole contributor.
> All changes must go through a pull request, which ensures the CI pipeline runs before any merge.

### 5.2 CI trigger configuration

The CI pipeline must be triggered on pull requests targeting `main` and `develop` to enable status check enforcement.

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main, develop]
```

Without the `pull_request` trigger, GitHub cannot block a merge on CI failure.

