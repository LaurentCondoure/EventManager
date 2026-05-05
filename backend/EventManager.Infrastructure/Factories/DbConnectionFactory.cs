using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Factories;

using System.Data;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;


namespace EventManager.Infrastructure.Factories;

/// <summary>
/// Factory for creating SQL Server database connections from the configured connection string.
/// </summary>
public class DbConnectionFactory(IOptions<DatabaseOptions> options) : IDbConnectionFactory
{
    private readonly DatabaseOptions _options = options.Value;

    /// <inheritdoc/>
    public IDbConnection CreateConnection() => new SqlConnection(_options.DefaultConnection);
}
