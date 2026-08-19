# TECH-001 — CI / Source Branch Check / INFRA

**Type:** Technical task
**Version:** V1
**Domain:** ci
**Feature:** source-branch-check
**Layer:** `infra`
**Parent story:** [STORY-001](story-001-project-infrastructure-setup.md)
**Priority:** `high`
**Status:** `in progress`

---

> **Placement rule:** This task is attached to STORY-001 — Project Infrastructure Setup,
> the technical story that groups all infrastructure prerequisites for V1.

---

## Purpose

The CI pipeline does not currently validate the source branch of a pull request. A PR from any branch can target `main` or `develop`, bypassing the branching strategy defined in `guidelines-release-001.md`.

This task adds a CI job that blocks unauthorized source branches before a merge can occur.

**Architectural reference:** `documentation/governance/guidelines-release-001.md`

---

## Description

Add a `check-source-branch` job to `.github/workflows/ci.yml`.

The job must:
- Run on every pull request event
- Check the source branch against the authorized patterns for the target branch
- Fail with a descriptive error message if the source branch is unauthorized
- Pass silently if the source branch is authorized

**Authorized patterns:**

| Target branch | Authorized source branches |
|---|---|
| `main` | `release-*`, `hotfix-*` |
| `develop` | `feature-*`, `docs-*`, `release-*`, `hotfix-*` |

**Job to add in `ci.yml`:**

```yaml
check-source-branch:
  name: Validate source branch
  runs-on: ubuntu-latest
  if: github.event_name == 'pull_request'
  steps:
    - name: Check source branch for main
      if: github.base_ref == 'main'
      run: |
        SOURCE="${{ github.head_ref }}"
        if [[ "$SOURCE" != release-* && "$SOURCE" != hotfix-* ]]; then
          echo "❌ Branch '$SOURCE' is not allowed to merge into main."
          echo "Only release-* and hotfix-* branches can target main."
          exit 1
        fi
        echo "✅ Source branch '$SOURCE' is authorized to target main."

    - name: Check source branch for develop
      if: github.base_ref == 'develop'
      run: |
        SOURCE="${{ github.head_ref }}"
        if [[ "$SOURCE" != feature-* && "$SOURCE" != docs-* && "$SOURCE" != release-* && "$SOURCE" != hotfix-* ]]; then
          echo "❌ Branch '$SOURCE' is not allowed to merge into develop."
          echo "Only feature-*, docs-*, release-* and hotfix-* branches can target develop."
          exit 1
        fi
        echo "✅ Source branch '$SOURCE' is authorized to target develop."
```

**CI trigger — verify the following is present in `ci.yml`:**

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main, develop]
```

After adding the job, add `check-source-branch` to the required status checks for both `main` and `develop` in GitHub Settings → Branches.

---

## Acceptance Criteria

- [ ] The `check-source-branch` job is present in `ci.yml`
- [ ] A PR from `docs-*` targeting `main` is blocked by the job
- [ ] A PR from `feature-*` targeting `main` is blocked by the job
- [ ] A PR from `release-*` targeting `main` passes the job
- [ ] A PR from `hotfix-*` targeting `main` passes the job
- [ ] A PR from `feature-*` targeting `develop` passes the job
- [ ] A PR from `docs-*` targeting `develop` passes the job
- [ ] `check-source-branch` is listed as a required status check on `main` and `develop`

---

## Implementation Notes

The job uses `github.head_ref` (source branch) and `github.base_ref` (target branch) — both are available in the pull request event context. Bash glob patterns (`release-*`) are used for prefix matching.

---

## Definition of Done

- [ ] All acceptance criteria are met
- [ ] ISO dev/prod verified — CI runs identically on all environments
- [ ] No hardcoded secrets or environment-specific code paths
- [ ] `guidelines-release-001.md` updated if the authorized branch patterns change
- [ ] Parent story STORY-001 acceptance criteria unblocked by this task

---

## Notes

This task was triggered by a merge of a `docs-*` branch directly into `main`, which bypassed the branching strategy. The CI job is the technical enforcement of the convention defined in `guidelines-release-001.md`.
