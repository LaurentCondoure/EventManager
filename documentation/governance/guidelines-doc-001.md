# Documentation Guidelines — Naming Conventions & Folder Structure

**Reference:** GUIDELINES-DOC-001
**Status:** Validated
**Date:** 2026-08-17

---

## Purpose

This document defines the naming conventions, folder structure, and expected content for every documentation file in the EventManager project. It applies to all versions without exception.

Any new documentation file must comply with these guidelines before being committed. The `Check-DocLinks.ps1` script enforces link integrity automatically on every push to `main`.

---

## 1. General Principles

- All filenames are in **kebab-case** — lowercase, words separated by hyphens.
- Every file type has a **prefix** that identifies its nature without opening the file.
- **Version numbers** appear in filenames only when the document is version-locked (scoping note, DoD, runbook, changelog). Living documents updated across versions carry no version number in their filename — their internal revision history tracks changes.
- **Sequential numbers** in story, task, tech, and bug files are **reset per type** within their subfolder.
- No file is created without a corresponding entry in this guideline. New file types must be proposed through a governance review before use.

---

## 2. Folder Structure

```
documentation/
  architecture/
    tat-[project].md
    data/
      [database]-mcd.md
      [database]-mld.md
    design/
      [domain]/
        design-[slug].md
    adr/
      adr-[NNN]-[slug].md
  api/
    api-[slug].md
  functional/
    [domain]/
      scoping-vX-[name].md
      dod-vX-[name].md
      vX-[name]/
        story-[NNN]-[slug].md
        task-[NNN]-[domain]-[feature]-[layer].md
        tech-[NNN]-[domain]-[feature]-[layer].md
        bug-[NNN]-[slug].md
  process/
    dor-template.md
    dod-template.md
    runbook-template.md
    story-template.md
    task-template.md
    tech-template.md
    bug-template.md
  governance/
    guidelines-doc-001.md        ← this document
  technical/
    tech-[slug].md
  changelog/
    changelog-vX.md
  runbooks/
    runbook-vX.md
```

---

## 3. Naming Conventions

### 3.1 Segments

| Segment | Description | Example |
|---|---|---|
| `vX` | Version number | `v1`, `v2` |
| `[NNN]` | Three-digit sequential number, reset per type per subfolder | `001`, `012` |
| `[slug]` | Short descriptive identifier in kebab-case | `user-management`, `superadmin-login` |
| `[domain]` | Business or technical domain | `auth`, `events`, `venue` |
| `[feature]` | Feature within the domain | `login`, `reservation` |
| `[layer]` | Technical layer (see Section 3.3) | `back`, `db` |
| `[database]` | Database name | `eventmanager` |
| `[project]` | Project name | `eventmanager` |

### 3.2 File prefixes

| Prefix | Type | Version in name | Example |
|---|---|---|---|
| `scoping-` | Scoping note | Yes | `scoping-v1-user-management.md` |
| `dod-` | Definition of Done | Yes | `dod-v1-user-management.md` |
| `tat-` | Technical Architecture Document | No | `tat-eventmanager.md` |
| `adr-` | Architecture Decision Record | No | `adr-001-authentication.md` |
| `design-` | Design document | No | `design-auth-flows.md` |
| `api-` | API reference | No | `api-events.md` |
| `runbook-` | Runbook | Yes | `runbook-v1.md` |
| `changelog-` | Changelog | Yes | `changelog-v1.md` |
| `tech-` | Technical documentation | No | `tech-link-validator.md` |
| `story-` | User story | No | `story-001-superadmin-login.md` |
| `task-` | Functional task | No | `task-001-auth-login-back.md` |
| `tech-` | Technical task | No | `tech-001-auth-identity-infra.md` |
| `bug-` | Bug report | No | `bug-001-login-redirect.md` |

> **Note:** The `tech-` prefix is shared between technical documentation (`technical/`) and technical tasks (`functional/[domain]/vX-[name]/`). The folder context disambiguates the type.

### 3.3 Layers

Used in task and technical task filenames.

| Layer | Scope |
|---|---|
| `back` | Backend / API |
| `front` | Frontend / UI |
| `test` | Test coverage |
| `infra` | Infrastructure, Docker, configuration |
| `db` | Database, migrations, schema |

---

## 4. File Content Descriptions

### 4.1 Technical Architecture Document — `tat-[project].md`

**Location:** `documentation/architecture/`
**Living document:** Yes — updated at each version. Previous version sections are immutable.
**Template:** No dedicated template — see structure below.

Single document for the entire project. Combines two reading levels:
- **System view** — global architecture, components, interactions, structural technology choices. Updated only when a structural change is introduced.
- **Version view** — one subsection per version documenting decisions, ADRs, diagrams, known limitations, and scalability assessment for that version.

Each version adds a new subsection. Past subsections are never modified.

---

### 4.2 Architecture Decision Record — `adr-[NNN]-[slug].md`

**Location:** `documentation/architecture/adr/`
**Living document:** No — immutable once validated.
**Template:** ADR format defined in the TAD.

Documents a single significant architectural decision: context, problem, alternatives considered, decision retained, justification, consequences, and accepted limitations.

If a decision is revised, a new ADR is created. The superseded ADR is marked `Superseded by ADR-NNN`.

Sequential number is global across the project — not reset per version.

---

### 4.3 Design Document — `design-[slug].md`

**Location:** `documentation/architecture/design/[domain]/`
**Living document:** Yes — updated when flows change, regardless of version.
**Template:** None — free structure, must include Mermaid diagrams inline.

Describes how components interact on specific technical flows. Contains Mermaid diagrams (sequence, activity, state, component) embedded inline. Organized by domain.

One design document per functional area (e.g. authentication flows, event creation flows). Updated when a new version modifies an existing flow — revision history tracks what changed and when.

---

### 4.4 Data Model — `[database]-mcd.md` and `[database]-mld.md`

**Location:** `documentation/architecture/data/`
**Living document:** Yes — updated when the schema changes.
**Template:** None — Mermaid diagrams inline.

One pair of files per database. The MCD (Conceptual Data Model) describes entities and relationships independently of implementation. The MLD (Logical Data Model) describes the physical schema derived from the MCD.

Both files are unique per database — no version in the filename. Revision history tracks schema evolution.

---

### 4.5 API Reference — `api-[slug].md`

**Location:** `documentation/api/`
**Living document:** Yes — updated when endpoint contracts change.
**Template:** None — structured by endpoint.

Documents the API contracts for a functional domain: base URL, endpoints, request parameters, request body, response codes, response body, and error format.

One file per functional domain. Updated when a new version adds or modifies an endpoint. References the relevant design document for flow context.

---

### 4.6 Scoping Note — `scoping-vX-[name].md`

**Location:** `documentation/functional/[domain]/`
**Living document:** No — validated once per version. Amendments are tracked in revision history.
**Template:** Scoping note format defined in project instructions.

Describes the perimeter of a version or feature: objective, in scope, out of scope, open decisions, acceptance criteria, and impact on existing versions. Includes a lightweight Definition of Ready as its final section.

One scoping note per version. A scoping note may span multiple versions if a feature is partially implemented — in this case it remains open and is referenced by stories across versions.

User stories reference the scoping note — not the other way around.

---

### 4.7 Definition of Done — `dod-vX-[name].md`

**Location:** `documentation/functional/[domain]/`
**Living document:** No — validated before development starts.
**Template:** `documentation/process/dod-template.md`

Defines the conditions to declare a version done (version DoD) and a story done (story DoD). Must be written and validated before development starts — it is a prerequisite listed in the version DoR.

Covers four dimensions: functional, quality, documentation, and operational.

---

### 4.8 Runbook — `runbook-vX.md`

**Location:** `documentation/runbooks/`
**Living document:** No — one runbook per version.
**Template:** `documentation/process/runbook-template.md`

Describes all operational procedures for a given version: startup, initial provisioning, shutdown, deployment, rollback, environment variables, backup and restore, and diagnostics. Covers both local and production environments.

Technology-agnostic structure — implementation details are version-specific.

---

### 4.9 Changelog — `changelog-vX.md`

**Location:** `documentation/changelog/`
**Living document:** No — one changelog per version.
**Template:** None.

Records what was delivered in a version: features, technical changes, bug fixes, known limitations, and a brief retrospective note on what deviated from plan. Written when the version is declared stable.

---

### 4.10 Technical Documentation — `tech-[slug].md`

**Location:** `documentation/technical/`
**Living document:** Yes — updated when the tool or process it describes changes.
**Template:** None.

Documents internal tools, scripts, and technical processes that support the project but are not part of the application itself (e.g. the link validation script, CI pipeline description). One file per tool or process.

---

### 4.11 User Story — `story-[NNN]-[slug].md`

**Location:** `documentation/functional/[domain]/vX-[name]/`
**Living document:** No — closed when done.
**Template:** `documentation/process/story-template.md`

Describes a functional requirement derived from the scoping note. Follows the format: As a… I want… so that…. Includes acceptance criteria, edge cases, out of scope items, dependencies, linked tasks, and a story-level Definition of Done.

Sequential number reset per subfolder. References its scoping note. Cannot be closed until all linked tasks are done.

---

### 4.12 Functional Task — `task-[NNN]-[domain]-[feature]-[layer].md`

**Location:** `documentation/functional/[domain]/vX-[name]/`
**Living document:** No — closed when done.
**Template:** `documentation/process/task-template.md`

Describes a concrete implementation unit derived from a user story during grooming. Has no user value on its own. Must reference a parent story. Includes implementation notes, abstraction constraints, and a task-level Definition of Done.

Sequential number reset per subfolder, independently from stories and technical tasks.

---

### 4.13 Technical Task — `tech-[NNN]-[domain]-[feature]-[layer].md`

**Location:** `documentation/functional/[domain]/vX-[name]/`
**Living document:** No — closed when done.
**Template:** `documentation/process/tech-template.md`

Describes infrastructure or technical work with no direct user value (e.g. EF Core migration, identity setup, Docker Compose authoring). Must be attached to the **first user story that requires the technical constraint it addresses** — not to a later story that assumes it is already in place.

Must reference a parent story. If a new technology is introduced, a corresponding ADR must be written. ISO dev/prod compliance is a mandatory criterion.

Sequential number reset per subfolder, independently from stories and functional tasks.

---

### 4.14 Bug — `bug-[NNN]-[slug].md`

**Location:** `documentation/functional/[domain]/vX-[name]/`
**Living document:** No — closed when fixed.
**Template:** `documentation/process/bug-template.md`

Documents a defect on an existing feature. Includes reproduction steps, expected vs actual behavior, root cause, and fix description. Must reference the story or feature it affects. The Definition of Done requires a test to prevent recurrence.

Sequential number reset per subfolder, independently from stories and tasks.

---

## 5. Process Templates

All templates are stored in `documentation/process/`. They must not be modified without a governance review.

| Template | Used for |
|---|---|
| `dor-template.md` | Definition of Ready — version and story levels |
| `dod-template.md` | Definition of Done — version and story levels |
| `runbook-template.md` | Runbook — operational procedures per version |
| `story-template.md` | User story |
| `task-template.md` | Functional task |
| `tech-template.md` | Technical task |
| `bug-template.md` | Bug report |

---

## 6. Link Integrity

All relative links between documents are validated automatically by `Check-DocLinks.ps1` on every push and pull request to `main`. A broken link fails CI.

Rules:
- Links must use relative paths from the file's location
- `http(s)://` links are skipped by the validator
- Moving or renaming a file requires updating all files that reference it

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-17 | Document created |
