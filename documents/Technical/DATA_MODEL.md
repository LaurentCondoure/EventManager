# Data Model — Events Management

**Version:** 1.0  
**Date:** 2026-04-19

---

## SQL Server (Structured data)

### Table: Events

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PRIMARY KEY | Unique identifier |
| Title | VARCHAR(200) | NOT NULL | Event title |
| Description | TEXT | NOT NULL | Full description |
| Date | DATETIME | NOT NULL | Event date and time |
| Location | VARCHAR(200) | NOT NULL | Location (city, venue) |
| Capacity | INT | NOT NULL, > 0 | Maximum capacity |
| Price | DECIMAL(10,2) | NOT NULL, >= 0 | Entry price |
| Category | VARCHAR(50) | NOT NULL | Event category |
| ArtistName | VARCHAR(200) | NULL | Artist/troupe name (optional) |
| CreatedAt | DATETIME | NOT NULL, DEFAULT GETUTCDATE() | Creation date |
| UpdatedAt | DATETIME | NULL | Last modification date |

**Indexes:**
- `IX_Events_Date` on `Date` column (frequent date-based queries)
- `IX_Events_Category` on `Category` column (future filters)

**Note on ArtistName:**
- Optional field to enrich events
- Simple approach without a separate Artists table (avoids M:N complexity)
- Post-MVP evolution: migrate to Artists table if complex relations are needed

---

### Table: Users

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | GUID | PRIMARY KEY | Unique identifier |
| Email | VARCHAR(200) | NOT NULL, UNIQUE | User email |
| Name | VARCHAR(100) | NOT NULL | User name |
| CreatedAt | DATETIME | NOT NULL, DEFAULT GETUTCDATE() | Creation date |

**Note:** Table prepared for future user management (bookings, authentication)

---

## MongoDB (Semi-structured data)

### Collection: event_comments

```javascript
{
  _id: ObjectId,       // MongoDB identifier
  eventId: GUID,       // Reference to Event (SQL Server)
  userId: GUID,        // Reference to User (SQL Server)
  userName: string,    // Display name (denormalized for performance)
  text: string,        // Comment text (optional)
  rating: int,         // Rating 1-5 (required)
  createdAt: datetime  // Creation date
}
```

**Indexes:**
- Index on `eventId` (frequent queries: retrieve comments for an event)
- Index on `createdAt` (descending sort)

**Why MongoDB for comments:**
- Semi-structured data (free text, variable length)
- No complex relations needed
- Easy future extensibility (add fields: likes, nested replies, metadata)
- No strict ACID transactions needed

---

## Primary Key Choice: GUID vs Auto-increment INT

**Decision:** Use GUID as primary key for Events and Users tables.

| Advantage | Description |
|-----------|-------------|
| **Distributed generation** | Generated client-side (.NET) without DB round-trip |
| **Data merge** | Facilitates merging multiple databases (no ID collision) |
| **Security** | Non-sequential IDs — not guessable in public URLs |
| **Future architecture** | Compatible with microservices |

**Accepted drawbacks:**

| Drawback | Impact | Mitigation |
|----------|--------|------------|
| Storage size | 16 bytes vs 4 bytes (INT) | Negligible for MVP volume |
| Index performance | Fragmentation (non-sequential) | Use NEWSEQUENTIALID() if needed |
| Readability | Less readable in logs | Acceptable |

**Implementation:**
```csharp
var newEvent = new Event
{
    Id = Guid.NewGuid(), // Generated in .NET, not in DB
    // ...
};
```
