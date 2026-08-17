# ADR-017: First Super Admin provisioning

**Status:** Accepted
**Version:** V1 — User Management & Containerisation
**Date:** 10/08/2026

---

## Context

There is no self-registration. The first Super Admin must exist before anyone can use the administration interface. This is a bootstrapping problem that must be solved at deployment time, without manual database intervention, and reproducibly across environments.

## Options considered

**Option A — Idempotent seed at API startup via environment variables**
The API reads `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD` at startup. If no Super Admin exists, it creates one and sets `must_reset_password = true`. If one already exists, the seed is skipped. Credentials are injected via Docker Compose env file, never hardcoded.
Disadvantages: initial password exists in plaintext in the env file.

**Option B — Separate CLI seed command**
A dedicated `dotnet run --seed` invocation after deployment.
Disadvantages: manual step, error-prone, not reproducible.

**Option C — Manual SQL insert**
Fragile, not reproducible, violates the abstraction principle.

## Decision

Option A: idempotent seed at API startup. Environment variables: `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD`. The provisioned account carries `must_reset_password = true`. Enforcement of this flag is defined in ADR-014.

The seed runs after EF Core migrations have completed, guaranteeing the identity schema is in place before the insert is attempted.

## Consequences

Zero manual steps at deployment. Fully reproducible across environments. The env file containing seed credentials must not be committed to version control (`.gitignore`).

## Accepted limitations

The seed password is in plaintext in the env file at rest on the server. Acceptable for a first internal production release.
