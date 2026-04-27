-- ============================================================
-- Migration 002 - Update event structure
-- Add columns Location and Text used for search (through ElasticSearch)
-- ============================================================

USE  EventManagement
GO

ALTER TABLE Events
ADD Location TEXT NOT NULL Default '';
ALTER TABLE Events
ADD ArtisteName TEXT NULL;

GO