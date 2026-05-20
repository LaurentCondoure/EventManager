-- ============================================================
-- Migration 003 - Application user
-- Requires sqlcmd variable: APP_PASSWORD
-- Run as sa (already the case via sql-init)
-- ============================================================

USE EventManagement;
GO

IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'eventmanagement_user')
BEGIN
    CREATE LOGIN eventmanagement_user WITH PASSWORD = '$(APP_PASSWORD)';
END
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'eventmanagement_user')
BEGIN
    CREATE USER eventmanagement_user FOR LOGIN eventmanagement_user;
    ALTER ROLE db_datareader ADD MEMBER eventmanagement_user;
    ALTER ROLE db_datawriter ADD MEMBER eventmanagement_user;
END
GO
