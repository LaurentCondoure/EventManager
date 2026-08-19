# User Stories — Epic 5: System Provisioning & ISO Dev/Prod

**Version:** V1 — User Management & Containerisation
**Status:** Ready

---

## US-025 — First super admin provisioning at startup

**As a system operator, the first super admin is provisioned automatically at application startup when no super admin exists, so that the application can be initialized without manual database intervention.**

- Reads `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD` from environment
- Account created with `IsActive = true` and `must_reset_password = true`
- If a super admin already exists → seed skipped silently
- Runs after EF Core migrations complete

---

## US-026 — Single-command local startup with ISO dev/prod

**As a developer or operator, the application starts with a single command locally using the same container image as in production, so that local and production environments are identical.**

- Single command starts the application and its database
- No environment-specific code path exists in the application
- Same Docker image used locally and in production
