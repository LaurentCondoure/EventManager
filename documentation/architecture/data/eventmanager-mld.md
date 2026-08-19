# EventManager — Logical Data Model (MLD)

**Reference:** eventmanager-mld
**Status:** Validated
**Database:** EventManager
**Engine:** SQL Server 2022
**ORM:** Entity Framework Core 8.0
**Last updated:** 2026-08-18

---

## Purpose

This document describes the logical and physical data model for the `EventManager` database.
It is derived from the conceptual model ([eventmanager-mcd.md](eventmanager-mcd.md)) and
describes the physical schema as implemented in SQL Server, including column types,
constraints, indexes, and EF Core mapping decisions.

---

## Version history

| Version | Changes |
|---|---|
| V1 | Initial schema. Migrated from Dapper (raw SQL) to EF Core (ADR-018). `Users` table removed — superseded by `EventManager_Identity`. Schema reproduced exactly from POC — no structural changes in V1. |

---

## Tables

### Events

```sql
CREATE TABLE Events (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    Title       NVARCHAR(200)       NOT NULL,
    Description NVARCHAR(MAX)       NOT NULL,
    Date        DATETIME2           NOT NULL,
    Location    NVARCHAR(200)       NOT NULL,
    Capacity    INT                 NOT NULL CHECK (Capacity > 0),
    Price       DECIMAL(10,2)       NOT NULL CHECK (Price >= 0),
    Category    NVARCHAR(50)        NOT NULL,
    ArtistName  NVARCHAR(200)       NULL,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NULL,

    CONSTRAINT PK_Events PRIMARY KEY (Id)
);
```

**Indexes:**

```sql
CREATE INDEX IX_Events_Date     ON Events (Date);
CREATE INDEX IX_Events_Category ON Events (Category);
```

**Column notes:**

| Column | Decision |
|---|---|
| `Id` | `UNIQUEIDENTIFIER` — generated in .NET via `Guid.NewGuid()`, not in the database. Consistent with POC choice. See MCD for trade-off analysis. |
| `Title`, `Description`, `Location`, `Category`, `ArtistName` | `NVARCHAR` — supports Unicode characters for cultural event names and descriptions. |
| `Date` | `DATETIME2` — higher precision and wider range than `DATETIME`. |
| `CreatedAt` | `DEFAULT GETUTCDATE()` — UTC timestamp set at row insertion. |
| `UpdatedAt` | Nullable — set by the application on modification, null on creation. |
| `ArtistName` | Nullable — optional denormalized field. No foreign key constraint. |

---

## MongoDB Collection

### event_comments

```javascript
{
  _id:       ObjectId,        // MongoDB auto-generated identifier
  eventId:   UUID,            // Logical reference to Events.Id (SQL Server)
  userId:    UUID,            // Logical reference to ApplicationUser.Id (EventManager_Identity)
  userName:  String,          // Denormalized display name — for read performance
  text:      String | null,   // Comment text — optional
  rating:    Number,          // Integer 1–5 — required
  createdAt: Date             // UTC timestamp
}
```

**Indexes:**

```javascript
db.event_comments.createIndex({ eventId: 1 })
db.event_comments.createIndex({ createdAt: -1 })
```

**Notes:**
- `eventId` references `Events.Id` — logical reference only, no enforced constraint across stores
- `userId` references `ApplicationUser.Id` in `EventManager_Identity` — logical reference only
- `userName` is denormalized to avoid cross-store joins on read

---

## EF Core Mapping

### EventManagerDbContext

```csharp
public class EventManagerDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Generated in .NET
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Capacity).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ArtistName).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired()
                  .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt);

            entity.HasIndex(e => e.Date).HasDatabaseName("IX_Events_Date");
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_Events_Category");
        });
    }
}
```

---

## Physical Diagram

> See [eventmanager-mld.drawio](eventmanager-mld.drawio) — open with Draw.io Integration (VS Code) or diagrams.net.

**Legend:**
- `Events` lives in SQL Server — `EventManager` database
- `event_comments` lives in MongoDB
- The dashed relationship is a logical cross-store reference, not an enforced foreign key constraint
- `idx` rows indicate indexed fields
- Cardinalities follow Merise notation: `(1,1) — (0,N)`

---

## Revision History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2026-08-18 | Document created — V1 initial schema from POC, migrated to EF Core |
