-- ============================================================
-- Migration 002 - Update event structure
-- Add columns Location and Text used for search (through ElasticSearch)
-- ============================================================

USE  EventManagement
GO

ALTER TABLE Events
ADD Location NVARCHAR(200) NOT NULL DEFAULT '';
ALTER TABLE Events
ADD ArtistName NVARCHAR(200) NULL;

GO