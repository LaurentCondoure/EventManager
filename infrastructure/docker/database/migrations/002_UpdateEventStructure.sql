-- ============================================================
-- Migration 002 - Update event structure
-- Add columns Location and Text used for search (through ElasticSearch)
-- ============================================================

USE  EventManagement
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Events' AND COLUMN_NAME = 'Location')
    ALTER TABLE Events ADD Location NVARCHAR(200) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Events' AND COLUMN_NAME = 'ArtistName')
    ALTER TABLE Events ADD ArtistName NVARCHAR(200) NULL;

GO