# Runbook — [Version name]

**Reference:** RUNBOOK-VX
**Status:** `Draft` / `Validated`
**Version:** [V X]
**Date:** [DD/MM/YYYY]
**Based on:** [Previous version] stable and in production

> This document describes the operational procedures for version [V X] of EventManager.
> It covers both local and production environments.
> For architecture context, see [dat-eventmanager.md](../architecture/tad-template).
> For deployment history, see [changelog-vX.md] [../changelog/changelog-vX.md].

---

## Prerequisites

List the tools, access rights, and conditions required before executing any procedure in this document.

| Requirement | Version | How to verify |
|---|---|---|
| [Tool or access] | [X.X+] | [Verification method] |
| [Tool or access] | [X.X+] | [Verification method] |

---

## 1. Startup

### 1.1 Local environment

**First launch — initial setup:**

1. [Step 1]
2. [Step 2]
3. [Step N]

**Expected state after startup:**

| Service | Expected status | How to verify |
|---|---|---|
| [Service 1] | [Expected state] | [Verification method] |
| [Service 2] | [Expected state] | [Verification method] |

**Subsequent launches:**

1. [Step 1]
2. [Step N]

### 1.2 Production environment

1. [Step 1]
2. [Step 2]
3. [Step N]

**Expected state after startup:**

| Service | Expected status | How to verify |
|---|---|---|
| [Service 1] | [Expected state] | [Verification method] |
| [Service 2] | [Expected state] | [Verification method] |

---

## 2. Initial Provisioning

*Steps to execute once, on first startup only. Each step must be idempotent or explicitly marked as non-idempotent.*

### 2.1 [Provisioning step — e.g. First administrator account]

**Local:**

1. [Step 1]
2. [Step N]

**Production:**

1. [Step 1]
2. [Step N]

> **Note:** [Idempotency, side effects, dependencies on other provisioning steps.]

### 2.2 [Other provisioning step if applicable]

[Same structure as above.]

---

## 3. Shutdown

### 3.1 Local environment

1. [Step 1]
2. [Step N]

### 3.2 Production environment

1. [Step 1]
2. [Step N]

### 3.3 Full reset (local only)

> **Warning:** This procedure destroys all local data. Never execute in production.

1. [Step 1]
2. [Step N]

---

## 4. Deployment

### 4.1 Standard deployment procedure

1. [Step 1]
2. [Step 2]
3. [Step N]
4. Verify: [how to confirm the deployment is successful]

### 4.2 Rollback procedure

> **When to rollback:** [Define the conditions that trigger a rollback — failed acceptance criterion, critical error within X minutes of startup, etc.]

1. [Step 1]
2. [Step 2]
3. [Step N]
4. Verify: [how to confirm the rollback is successful]

---

## 5. Configuration and Secrets

List all configuration variables required to run the application.

| Variable | Description | Required | Example |
|---|---|---|---|
| `[VARIABLE]` | [Description] | Yes / No | `[non-sensitive example]` |
| `[VARIABLE]` | [Description] | Yes / No | `[non-sensitive example]` |

> **Rules:**
> - No secret is hardcoded in source files or committed to version control.
> - In production, secrets are injected via [mechanism defined in ADR-XXX].
> - The local configuration file is excluded from version control.

---

## 6. Backup and Restore

### 6.1 Backup

**Scope:** [What is backed up — database, files, configuration, etc.]

**Local:**

1. [Step 1]
2. [Step N]

**Production:**

1. [Step 1]
2. [Step N]

> **Frequency:** [Daily / On each deployment / etc.]
> **Retention:** [X days / versions]
> **Storage location:** [Where backups are stored]

### 6.2 Restore

> **Warning:** Restore overwrites existing data. Confirm the backup integrity before proceeding.

1. [Step 1]
2. [Step N]
3. Verify: [how to confirm data integrity after restore]

---

## 7. Diagnostics

### 7.1 Check service status

[Describe how to verify that each service is operational — commands, URLs, expected responses.]

| Service | How to check | Expected response |
|---|---|---|
| [Service 1] | [Method] | [Expected output] |
| [Service 2] | [Method] | [Expected output] |

### 7.2 Access logs

[Describe how to access logs for each service — location, command, format.]

| Service | Log location / command | Notes |
|---|---|---|
| [Service 1] | [Location or command] | [Retention, format, etc.] |
| [Service 2] | [Location or command] | [Retention, format, etc.] |

### 7.3 Common issues

| Symptom | Probable cause | Resolution |
|---|---|---|
| [Symptom] | [Cause] | [Step-by-step resolution] |
| [Symptom] | [Cause] | [Step-by-step resolution] |

---

## 8. Known Limitations

*Operational limitations consciously accepted for this version.*
*For architectural limitations, see the DAT.*

| Limitation | Operational impact | Target resolution |
|---|---|---|
| [Limitation] | [Impact] | [V X or TBD] |

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | [DD/MM/YYYY] | Document created |
