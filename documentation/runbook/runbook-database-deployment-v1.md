# Runbook - Database Deployment V1

**Reference:** RUNBOOK-DB-001  
**Status:** `Draft`  
**Version:** V1  
**Date:** 26/08/2026  
**Scope:** SQL Server databases `EventManager` and `EventManager_Identity`

This runbook describes the provisioning and deployment procedure for the EF Core databases. The API applies pending migrations at startup with dedicated migration connections. Runtime connections are used only for normal application access.

---

## Prerequisites

| Requirement | How to verify |
|---|---|
| SQL Server instance available | Connect with SSMS or `Test-NetConnection <host> -Port 1433` |
| SQL Server administrator access | Connect with an administrative login such as `sa` |
| API configuration access | User Secrets locally or deployment secret store |
| Compiled API containing the migrations | `backend/EventManager.Infrastructure/Migrations/Events` and `Identity` exist |

The databases must exist before the API starts. Database creation and login provisioning are administrative operations and are not performed by the EF migrations.

## 1. Provision the databases

Execute as a SQL Server administrator in SSMS:

```sql
USE [master];
GO

IF DB_ID(N'EventManager') IS NULL
    CREATE DATABASE [EventManager];
GO

IF DB_ID(N'EventManager_Identity') IS NULL
    CREATE DATABASE [EventManager_Identity];
GO
```

This operation is idempotent. Do not drop existing databases during a normal deployment.

## 2. Provision logins and database users

Create four separate logins. Use strong passwords and provide them through the secret store; never commit them to the repository.

```sql
USE [master];
GO

CREATE LOGIN [eventmanagement_user]
WITH PASSWORD = '<runtime-events-password>';
CREATE LOGIN [identity_user]
WITH PASSWORD = '<runtime-identity-password>';
CREATE LOGIN [eventmanager_migrator]
WITH PASSWORD = '<migration-events-password>';
CREATE LOGIN [identity_migrator]
WITH PASSWORD = '<migration-identity-password>';
GO
```

If a login may already exist, replace the `CREATE LOGIN` statements with an idempotent `IF NOT EXISTS` block and manage password rotation separately.

Associate the logins with their databases:

```sql
USE [EventManager];
GO

CREATE USER [eventmanagement_user] FOR LOGIN [eventmanagement_user];
CREATE USER [eventmanager_migrator] FOR LOGIN [eventmanager_migrator];
ALTER ROLE [db_datareader] ADD MEMBER [eventmanagement_user];
ALTER ROLE [db_datawriter] ADD MEMBER [eventmanagement_user];
ALTER ROLE [db_ddladmin] ADD MEMBER [eventmanager_migrator];
GO

USE [EventManager_Identity];
GO

CREATE USER [identity_user] FOR LOGIN [identity_user];
CREATE USER [identity_migrator] FOR LOGIN [identity_migrator];
ALTER ROLE [db_datareader] ADD MEMBER [identity_user];
ALTER ROLE [db_datawriter] ADD MEMBER [identity_user];
ALTER ROLE [db_ddladmin] ADD MEMBER [identity_migrator];
GO
```

The runtime accounts have data access only. The migration accounts require DDL access to create and update tables, indexes, constraints and `__EFMigrationsHistory`.

## 3. Configure connection strings

Configure these values in User Secrets for local development or in the deployment secret store for deployed environments:

| Key | Database | Account |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `EventManager` | `eventmanagement_user` |
| `ConnectionStrings:IdentityConnection` | `EventManager_Identity` | `identity_user` |
| `ConnectionStrings:DefaultMigrationConnection` | `EventManager` | `eventmanager_migrator` |
| `ConnectionStrings:IdentityMigrationConnection` | `EventManager_Identity` | `identity_migrator` |

Example local commands:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=EventManager;User Id=eventmanagement_user;Password=<runtime-events-password>;TrustServerCertificate=True" --project backend/EventManager.Api
dotnet user-secrets set "ConnectionStrings:IdentityConnection" "Server=localhost,1433;Database=EventManager_Identity;User Id=identity_user;Password=<runtime-identity-password>;TrustServerCertificate=True" --project backend/EventManager.Api
dotnet user-secrets set "ConnectionStrings:DefaultMigrationConnection" "Server=localhost,1433;Database=EventManager;User Id=eventmanager_migrator;Password=<migration-events-password>;TrustServerCertificate=True" --project backend/EventManager.Api
dotnet user-secrets set "ConnectionStrings:IdentityMigrationConnection" "Server=localhost,1433;Database=EventManager_Identity;User Id=identity_migrator;Password=<migration-identity-password>;TrustServerCertificate=True" --project backend/EventManager.Api
```

In PowerShell, use single quotes around a command argument when a password contains `$`:

```powershell
dotnet user-secrets set 'ConnectionStrings:DefaultMigrationConnection' 'Server=localhost,1433;Database=EventManager;User Id=eventmanager_migrator;Password=contains$;TrustServerCertificate=True' --project backend/EventManager.Api
```

## 4. Apply migrations

Start SQL Server and wait until it is healthy:

```powershell
docker compose -f infrastructure/docker/docker-compose.yml up -d sqlserver
docker ps
```

Start the API:

```powershell
dotnet run --project backend/EventManager.Api --launch-profile https
```

`DatabaseMigrationHostedService` applies the event migration first and the Identity migration second. It logs whether each database was already current, which migrations are pending, and whether application succeeded. The API starts serving requests only after hosted service startup completes.

## 5. Verify deployment

In SSMS, verify the migration history in each database:

```sql
USE [EventManager];
SELECT * FROM [dbo].[__EFMigrationsHistory];
SELECT name FROM sys.tables WHERE name = N'Events';
GO

USE [EventManager_Identity];
SELECT * FROM [dbo].[__EFMigrationsHistory];
SELECT name FROM sys.tables
WHERE name IN (N'AspNetUsers', N'AspNetRoles', N'RefreshTokens', N'PasswordHistory');
GO
```

Expected migrations:

```text
InitialEvents
InitialIdentity
```

Also check the API logs for:

```text
EF Core migrations: EventManager applied successfully
EF Core migrations: EventManager_Identity applied successfully
```

Finally verify the API health endpoint through the configured URL.

## 6. Rollback

Do not delete or edit a migration that has been applied to a shared database. Create a new corrective migration and deploy it forward.

If an unapplied migration must be reviewed, generate its SQL script and validate it before execution. Database restore is the rollback mechanism for destructive production changes; take and verify a backup before deployment.

## 7. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| `Login failed for user` | Wrong password, login disabled, or wrong server | Test the same server/login in SSMS and compare the secret key used by the API |
| `CREATE DATABASE permission denied` | Migration account is being used to create the database | Provision the databases administratively before starting the API |
| `There is already an object named 'Events'` | Existing database has no matching EF history | Baseline or reconcile the existing database before applying `InitialEvents` |
| API does not start after SQL changes | Migration failed | Read the hosted service error log and inspect the database state before retrying |

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 26/08/2026 | Initial database provisioning and EF Core migration runbook |