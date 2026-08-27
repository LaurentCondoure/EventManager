using EventManager.Infrastructure.Identity;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace EventManager.InfrastructureTests.Fixtures;

/// <summary>
/// Shares one SQL Server container across a test class, handing out a fresh, already-migrated
/// <c>EventManager_Identity</c> database per test so tests don't interfere via shared state.
/// </summary>
public sealed class IdentitySqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public async Task<string> CreateMigratedIdentityDatabaseAsync()
    {
        string databaseName = $"EventManager_Identity_ProvisioningTests_{Guid.NewGuid():N}";

        await using (var connection = new SqlConnection(BuildConnectionString("master")))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        string connectionString = BuildConnectionString(databaseName);

        var options = new DbContextOptionsBuilder<EventManagerIdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var context = new EventManagerIdentityDbContext(options);
        await context.Database.MigrateAsync();

        return connectionString;
    }

    private string BuildConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }
}
