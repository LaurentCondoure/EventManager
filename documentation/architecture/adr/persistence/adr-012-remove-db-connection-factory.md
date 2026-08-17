# ADR-012: Remove IDbConnectionFactory

## Status
Accepted

## Context

`IDbConnectionFactory` was introduced to abstract database connection creation in
`SqlServerEventRepository`. Its initial purpose was to allow the test suite to swap the
underlying provider: in early development, tests used SQLite in-memory via the factory
rather than a real SQL Server instance.

When the test strategy moved to Testcontainers (ADR-010), SQLite was removed from the
factory. The factory was kept as a forward-compatibility mechanism: if a second relational
provider (e.g. PostgreSQL) were to join the project, the factory would allow swapping
providers without changing repository code.

## Alternatives Considered

### Keep IDbConnectionFactory

Retain the abstraction to accommodate a potential future provider (PostgreSQL or other).

**Rejected because:** no multi-provider requirement is planned before several major versions.
An abstraction that serves no current purpose adds indirection without benefit. YAGNI applies.
The factory can be reintroduced when a concrete need arises — it is a straightforward change.

### Remove IDbConnectionFactory (chosen)

Inject `IOptions<DatabaseOptions>` directly into `SqlServerEventRepository` and instantiate
`SqlConnection` inline. Testability is preserved: the connection string remains externalized
and injectable via `Options.Create(...)` in test fixtures.

## Decision

`IDbConnectionFactory` and `DbConnectionFactory` are removed. `SqlServerEventRepository`
takes `IOptions<DatabaseOptions>` directly:

```csharp
public class SqlServerEventRepository(IOptions<DatabaseOptions> options) : IEventRepository
{
    private IDbConnection CreateConnection() => new SqlConnection(options.Value.DefaultConnection);
}
```

The factory will be reintroduced if a multi-provider requirement is confirmed in a future
major version.

## Consequences

- One fewer abstraction in the Infrastructure layer
- DI registration simplified (one line removed from `Program.cs`)
- Test fixtures unchanged in intent — connection string injection preserved via `IOptions`
