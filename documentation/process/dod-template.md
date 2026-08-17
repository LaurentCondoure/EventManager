# Definition of Done — Template

**Reference:** DOD-TEMPLATE-001
**Status:** `Draft` / `Validated`
**Version:** [V X.Y]
**Date:** [DD/MM/YYYY]
**Based on scoping note:** [Scoping note reference]
**Based on TAD:** [TAD reference]

---

## Purpose

The Definition of Done (DoD) defines the conditions that must be met before a work item can be considered complete. Its role is to ensure a shared and objective understanding of "done" across all contributors, and to prevent incomplete work from being declared finished.

> **Usage rules**
> - This document is a template. It must be instantiated for each version.
> - Two DoD levels coexist: version level (conditions to declare the entire version stable and in production) and story level (conditions to declare an individual user story done).
> - A work item that does not satisfy its DoD is not done — without exception.
> - The DoD must be written and validated before development starts. It is a prerequisite listed in the version DoR.
> - Any waiver must be documented, justified, and counter-signed.

---

## Part 1 — Version-Level DoD

The version DoD is evaluated once, before declaring the version stable and ready for production. It ensures that all delivery conditions are met across functional, technical, and operational dimensions.

### 1.1 Functional

| Criterion | Owner | ✓ |
|---|---|---|
| All acceptance criteria from the scoping note are met | PM | ☐ |
| All user stories in scope are completed and individually validated | PM | ☐ |
| No regression has been introduced on previous version features | Tech Lead | ☐ |
| Out of scope items have not been implemented | PM | ☐ |

### 1.2 Quality

| Criterion | Produced by | Approved by | ✓ |
|---|---|---|---|
| Unit tests cover all business-critical paths | Tech Lead | Lead Architect | ☐ |
| Integration tests cover all cross-component flows | Tech Lead | Lead Architect | ☐ |
| All tests pass in CI | Tech Lead | Lead Architect | ☐ |
| No known blocker or critical bug remains open | Tech Lead | Lead Architect | ☐ |
| Code has been reviewed against architecture decision document | Tech Lead | Lead Architect | ☐ |
| No abstraction interface has been bypassed | Tech Lead | Lead Architect | ☐ |
| No hardcoded secret exists in the codebase | Tech Lead | Lead Architect | ☐ |

### 1.3 Documentation

| Criterion | Produced by | Approved by | ✓ |
|---|---|---|---|
| TAD is updated and validated for this version | Lead Architect | CTO | ☐ |
| All ADRs for this version are written and referenced | Lead Architect | CTO | ☐ |
| Design documents are updated if flows have changed | Lead Architect | CTO | ☐ |
| API documentation is updated if contracts have changed | Tech Lead | Lead Architect | ☐ |
| Runbook is written and validated for this version | Tech Lead | Lead Architect | ☐ |
| Changelog entry is written for this version | Tech Lead | PM | ☐ |

### 1.4 Operational

| Criterion | Produced by | Approved by | ✓ |
|---|---|---|---|
| The application starts successfully in a clean local environment | Tech Lead | Lead Architect | ☐ |
| The application starts successfully in production environment | Tech Lead | Lead Architect | ☐ |
| Initial provisioning procedure has been validated | Tech Lead | Lead Architect | ☐ |
| Environment is ISO dev/prod | Tech Lead | CTO | ☐ |
| All environment variables are documented in the runbook | Tech Lead | Lead Architect | ☐ |

### 1.5 Version Completion Decision

> All boxes above must be checked before the version can be declared stable and in production.
> If any box is unchecked, block the release and document the reason below.

```
Decision:
  ☐ Done — version is stable and ready for production
  ☐ Not done — remaining item(s): _________________________________

Signed by: ________________________  Date: _______________
```

---

## Part 2 — Story-Level DoD

The story DoD is evaluated individually for each user story before it is declared done. It ensures that no story is considered complete without meeting quality and documentation standards.

### 2.1 Functional

| Criterion | Owner | ✓ |
|---|---|---|
| All acceptance criteria defined in the story card are met | PM | ☐ |
| All identified edge cases are handled | Tech Lead | ☐ |
| All defined error messages are implemented as specified | Tech Lead | ☐ |
| No behaviour outside the story scope has been implemented | PM / Tech Lead | ☐ |

### 2.2 Quality

| Criterion | Owner | ✓ |
|---|---|---|
| Unit tests are written and pass for this story | Tech Lead | ☐ |
| Integration tests are written and pass if the story touches a cross-component flow | Tech Lead | ☐ |
| The story introduces no regression on existing features | Tech Lead | ☐ |
| Code has been reviewed (self-review minimum, peer review if available) | Tech Lead | ☐ |
| No abstraction interface has been bypassed | Tech Lead | ☐ |
| Error handling is implemented on all paths | Tech Lead | ☐ |

### 2.3 Documentation

| Criterion | Owner | ✓ |
|---|---|---|
| API documentation is updated if the story introduces or modifies an endpoint | Tech Lead | ☐ |
| Design document is updated if the story modifies a documented flow | Tech Lead | ☐ |
| Technical debt introduced by the story is explicitly named and logged | Tech Lead | ☐ |

### 2.4 Standard Story Completion Card

*To be filled when closing a story. Attach to the backlog or tracking tool alongside the story card.*

```
Story ID       :
Title          :
Version        :

Acceptance criteria — all met:
  ☐ 1.
  ☐ 2.
  ☐ 3.

Edge cases — all handled  : ☐
Error handling — complete  : ☐
Tests passing              : ☐
No regression              : ☐
No scope creep             : ☐

Technical debt introduced:
  ☐ None
  ☐ Yes — description: _______________________________________________
         logged in   : _______________________________________________

DoD validated  : ☐   By: _______________  Date: _______________
```

---

## Part 3 — Validation Responsibilities

| Level | Produced by | Approved by | When |
|---|---|---|---|
| Version DoD — Functional | PM | PM | Before release |
| Version DoD — Quality | Tech Lead | Lead Architect | Before release |
| Version DoD — Documentation | Lead Architect / Tech Lead | CTO / Lead Architect | Before release |
| Version DoD — Operational | Tech Lead | CTO | Before release |
| Story DoD | Tech Lead | PM + Tech Lead | Before closing the story |

> **Note — small team / solo context**
> When no dedicated Lead Architect exists, the CTO role absorbs architecture approval. Apply a minimum 24-hour delay between producing and self-approving to preserve critical distance.

---

## Part 4 — DoD and DoR Relationship

The DoD and DoR are complementary gates on the same work item:

| Gate | Question answered | When evaluated |
|---|---|---|
| DoR | Is this item ready to start? | Before development begins |
| DoD | Is this item truly complete? | Before the item is declared done |

A story that satisfies its DoR but not its DoD is in progress, not done.
A version that satisfies its DoR but not its DoD has not been delivered.

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | [DD/MM/YYYY] | Document created |
