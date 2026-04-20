-- ============================================================
-- Migration 001 - Initial schema
-- Run this script against your SQL Server Express instance
-- Database files will be created on D:\
-- ============================================================

USE master;
GO

-- Create database with data files on D:\
CREATE DATABASE EventManagement
ON PRIMARY
(
    NAME = 'EventManagement',
    FILENAME = 'D:\SqlData\EventManagement.mdf',
    SIZE = 64MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 64MB
)
LOG ON
(
    NAME = 'EventManagement_log',
    FILENAME = 'D:\SqlData\EventManagement_log.ldf',
    SIZE = 16MB,
    MAXSIZE = 2048MB,
    FILEGROWTH = 16MB
);
GO

USE EventManagement;
GO

-- Events table
CREATE TABLE Events
(
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    Title       NVARCHAR(200)   NOT NULL,
    Description NVARCHAR(2000)  NOT NULL,
    Date        DATETIME2       NOT NULL,
    Venue       NVARCHAR(300)   NOT NULL,
    Capacity    INT             NOT NULL,
    Price       DECIMAL(10, 2)  NOT NULL,
    Category    NVARCHAR(100)   NOT NULL,
    CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2       NULL
);
GO

CREATE INDEX IX_Events_Date     ON Events (Date);
CREATE INDEX IX_Events_Category ON Events (Category);
GO

-- Reservations table
CREATE TABLE Reservations
(
    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    EventId     INT             NOT NULL,
    UserEmail   NVARCHAR(256)   NOT NULL,
    SeatCount   INT             NOT NULL,
    Status      TINYINT         NOT NULL DEFAULT 1,  -- 1=Confirmed, 2=Cancelled
    CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2       NULL,

    CONSTRAINT FK_Reservations_Events FOREIGN KEY (EventId) REFERENCES Events (Id),
    CONSTRAINT CK_Reservations_SeatCount CHECK (SeatCount > 0),
    CONSTRAINT CK_Reservations_Status CHECK (Status IN (1, 2))
);
GO

CREATE INDEX IX_Reservations_EventId   ON Reservations (EventId);
CREATE INDEX IX_Reservations_UserEmail ON Reservations (UserEmail);
GO

-- Seed data
INSERT INTO Events (Title, Description, Date, Venue, Capacity, Price, Category)
VALUES
    ('Les Misérables', 'La comédie musicale incontournable', '2026-06-15 20:00', 'Palais des Congrès, Paris', 3000, 89.00, 'Comédie musicale'),
    ('Orchestre National de France', 'Concert symphonique — Beethoven, Symphonie n°9', '2026-06-20 20:30', 'Salle Pleyel, Paris', 1900, 45.00, 'Musique classique'),
    ('Festival Jazz à Vienne', 'Grande soirée jazz avec artistes internationaux', '2026-07-05 21:00', 'Théâtre antique de Vienne', 8000, 35.00, 'Jazz');
GO
