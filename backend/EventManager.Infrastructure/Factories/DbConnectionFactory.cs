using Dapper;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.TypeHandlers;

using System.Data;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;



namespace EventManager.Infrastructure.Factories;

/// <summary>
/// Factory for creating database connections based on the configured provider and connection string.
/// Supports multiple providers (e.g., SQL Server, SQLite) and abstracts the connection creation logic
/// </summary>
/// <param name="options"></param>
/// <param name="provider"></param>
public class DbConnectionFactory(IOptions<DatabaseOptions> options, DbProvider provider) : IDbConnectionFactory
{
    private readonly DatabaseOptions _options = options.Value;
    private readonly DbProvider _provider = provider;

    // Ensures the SQLite type handlers are registered only once across the process lifetime.
    private static int _sqliteHandlersRegistered = 0;

    /// <summary>
    /// Creates and returns a new database connection based on the configured provider and connection string.
    /// </summary>
    /// <returns></returns>
    public IDbConnection CreateConnection()
    {
        if (_provider is DbProvider.Sqlite or DbProvider.InMemorySqlite)
        {
            // SqlMapper.AddTypeHandler is process-wide: registered once on first SQLite connection, affects all Dapper type mappings.
            // /!\ For multi provider, the type handler is common to all connections, regardless of the provider.
            if (Interlocked.CompareExchange(ref _sqliteHandlersRegistered, 1, 0) == 0)
                SqlMapper.AddTypeHandler(new GuidTypeHandler());

            return new SqliteConnection(_options.DefaultConnection);
        }

        return new SqlConnection(_options.DefaultConnection);
    }
}
