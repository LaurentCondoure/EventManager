# ADR-002: GUID as Primary Key Strategy

## Status
Accepted

## Context
During data model design, the primary key strategy for the `Events` and `Users` tables had to be defined. Two options were considered: auto-increment INT (IDENTITY) and GUID (UNIQUEIDENTIFIER).

The application exposes public REST APIs where entity identifiers appear in URLs (`/api/events/{id}`). A plausible evolution is a microservices architecture where each service generates its own identifiers without coordination. The current scope is a single SQL Server database with low volume (< 10,000 rows).

## Decision
GUID is used as the primary key for the `Events` and `Users` tables. Identifiers are generated client-side in .NET (`Guid.NewGuid()`) rather than by SQL Server (`NEWID()`), to keep full control over the identifier before the database round-trip.

```csharp
var newEvent = new Event
{
    Id = Guid.NewGuid(), // Generated in .NET, not in DB
    // ...
};
```

## Consequences
- Public URLs expose non-sequential, non-guessable identifiers — reduces enumeration risk on public APIs.
- Client-side generation eliminates a database round-trip to retrieve the generated key after insert.
- Compatible with a future microservices architecture where each service generates its own identifiers without central coordination.
- Facilitates data merge across multiple databases with no risk of ID collision.
- Index fragmentation is higher than with sequential INT keys — mitigated by `NEWSEQUENTIALID()` if performance becomes a concern at higher volumes.
- Storage cost is 16 bytes vs 4 bytes per key — negligible at MVP volume, relevant above 100M rows.
- GUIDs are less readable in logs and debug output — accepted as a minor operational inconvenience.
