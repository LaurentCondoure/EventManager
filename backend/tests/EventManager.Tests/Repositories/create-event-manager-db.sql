-- ============================================================
-- Migration 002 - GUID primary keys + ArtistName + Location + Users
-- SQLite version
-- ============================================================

DROP TABLE IF EXISTS Reservations;
DROP TABLE IF EXISTS Events;
DROP TABLE IF EXISTS Users;

CREATE TABLE Events
(
    Id          TEXT   NOT NULL PRIMARY KEY,
    Title       TEXT      NOT NULL,
    Description TEXT      NOT NULL,
    Date        TEXT      NOT NULL,
    Location    TEXT      NOT NULL,
    Capacity    INTEGER   NOT NULL,
    Price       NUMERIC   NOT NULL,
    Category    TEXT      NOT NULL,
    ArtistName TEXT      NOT NULL,
    CreatedAt   TEXT      NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    UpdatedAt   TEXT      NULL,
    CONSTRAINT CK_Events_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_Events_Price    CHECK (Price >= 0)
);

CREATE INDEX IX_Events_Date     ON Events (Date);
CREATE INDEX IX_Events_Category ON Events (Category);

CREATE TABLE Reservations
(
    Id        TEXT     NOT NULL PRIMARY KEY,
    EventId   TEXT     NOT NULL,
    UserEmail TEXT     NOT NULL,
    SeatCount INTEGER  NOT NULL,
    Status    INTEGER  NOT NULL DEFAULT 1,
    CreatedAt TEXT     NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    UpdatedAt TEXT     NULL,
    CONSTRAINT FK_Reservations_Events FOREIGN KEY (EventId) REFERENCES Events (Id),
    CONSTRAINT CK_Reservations_SeatCount CHECK (SeatCount > 0),
    CONSTRAINT CK_Reservations_Status    CHECK (Status IN (1, 2))
);

CREATE INDEX IX_Reservations_EventId   ON Reservations (EventId);
CREATE INDEX IX_Reservations_UserEmail ON Reservations (UserEmail);

INSERT INTO Events (Id, Title, Description, Date, Location, Capacity, Price, Category, ArtistName)
VALUES
    ('00000000-0000-0000-0000-000000000001', 'Les Misérables',
     'La comédie musicale incontournable tirée du roman de Victor Hugo.',
     '2026-06-15 20:00', 'Paris, Olympia', 3000, 89.00, 'Comédie musicale', 'Les Misérables Artist'),

    ('00000000-0000-0000-0000-000000000002', 'Orchestre National de France — Beethoven',
     'Concert symphonique : Symphonie n°9 de Beethoven dirigée par Klaus Mäkelä.',
     '2026-06-20 20:30', 'Paris, Philharmonie',  1900, 45.00, 'Musique classique', 'Orchestre National de France'),

    ('00000000-0000-0000-0000-000000000003', 'Festival Jazz à Vienne',
     'Grande soirée jazz avec des artistes internationaux dans le cadre exceptionnel du théâtre antique.',
     '2026-07-05 21:00', 'Vienne, Théâtre Antique',  8000, 35.00, 'Jazz', 'Festival Jazz à Vienne');