namespace EventManager.Infrastructure.Options;

/// <summary>
/// Supported database providers for the connection factory.
/// </summary>
public enum DbProvider
{
    SqlServer,
    Sqlite,
    InMemorySqlite
}
