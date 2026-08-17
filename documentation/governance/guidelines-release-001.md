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
        └── feature-[slug]
              └── [optional] subtask-[slug]
```

### 1.2 Branch definitions

| Branch | Purpose | Created from | Merged into |
|---|---|---|---|
| `main` | Stable production code. Every commit on `main` represents a released version. | — | — |
| `develop` | Integration branch. Accumulates completed features for the current version. | `main` | `main` (at release) |
| `feature-[slug]` | Implements a user story or a group of related tasks. | `develop` | `develop` |
| `subtask-[slug]` | Implements a single task or technical task within a feature. | `feature-[slug]` | `feature-[slug]` |

### 1.3 Naming conventions

| Branch | Pattern | Example |
|---|---|---|
| Feature | `feature-[slug]` | `feature-superadmin-login` |
| Subtask | `subtask-[slug]` | `subtask-identity-setup` |
| Hotfix | `hotfix-[slug]` | `hotfix-login-redirect` |

### 1.4 Rules

- `main` is protected — no direct commit. Only merge from `develop` (release) or `hotfix-*` (urgent fix).
- `develop` is protected — no direct commit. Only merge from `feature-*` branches via pull request.
- A `feature-[slug]` branch maps to one user story. If a story requires subtasks, each subtask gets its own branch created from the feature branch.
- A `feature-[slug]` branch is deleted after merge into `develop`.
- A `subtask-[slug]` branch is deleted after merge into its parent `feature-[slug]`.
- Branch names are in kebab-case.

### 1.5 Version lifecycle on branches

```
[V1 development]
  develop ←── feature-superadmin-login
                  └── subtask-identity-setup
                  └── subtask-ef-core-migration

[V1 release]
  develop ──► main   (version tag: v1.0.0)
```

A version is closed when `develop` is merged into `main`. The merge commit on `main` is tagged with the version number.

---

## 2. Release Process

### 2.1 Overview

```
Development complete on develop
  └── Release gates verified (Section 3)
        └── develop merged into main
              └── Version tag created on main
                    └── Deployment to production
                          └── Version declared stable
```

### 2.2 Step-by-step procedure

**Step 1 — Verify release gates**

All release gates defined in Section 3 must be satisfied before initiating the release. Do not proceed if any gate fails.

**Step 2 — Prepare the changelog**

Write or finalize `documentation/changelog/changelog-vX.md`. The changelog must be committed to `develop` before the release merge.

**Step 3 — Merge develop into main**

```
git checkout main
git merge --no-ff develop -m "release: vX.Y.Z"
```

The `--no-ff` flag preserves the merge commit and makes the release boundary explicit in the history.

**Step 4 — Tag the release**

```
git tag -a vX.Y.Z -m "Release vX.Y.Z — [Version name]"
git push origin main --tags
```

Versioning follows semantic versioning (semver):
- `X` — major version (breaking change or major functional increment)
- `Y` — minor version (new features, no breaking change)
- `Z` — patch version (bug fix or hotfix)

**Step 5 — Deploy to production**

Follow the deployment procedure defined in `documentation/runbooks/runbook-vX.md`.

**Step 6 — Verify production**

Validate that the deployment is successful using the verification steps defined in the runbook. If verification fails, execute the rollback procedure immediately.

**Step 7 — Declare the version stable**

Once production is verified, update the TAD and the scoping note status to `Validated`. The version is now stable and the next version scoping can begin.

### 2.3 Hotfix process

A hotfix addresses a critical defect discovered in production that cannot wait for the next version.

```
main ──► hotfix-[slug]
              └── fix committed
                    └── merged into main (tag: vX.Y.Z+1)
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

### 3.2 Security gate

> **Status: Deferred**
> Security gates will be defined and added to this section once the authentication ADR is validated.
> They will be integrated as part of the CI pipeline at the end of the version that introduces authentication.

### 3.3 Release authorization

```
All automated gates green  : ☐
All manual gates validated  : ☐
Security gate               : ☐ Deferred to end of version

Release authorized          : ☐
Authorized by               : _______________  Date: _______________
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
