using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace EventManager.InfrastructureTests.Fixtures;

public sealed class MigrationSqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string EventConnectionString { get; private set; } = string.Empty;

    public string IdentityConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        string suffix = Guid.NewGuid().ToString("N");
        string eventDatabase = $"EventManager_MigrationTests_{suffix}";
        string identityDatabase = $"EventManager_Identity_MigrationTests_{suffix}";

        await CreateDatabaseAsync(eventDatabase);
        await CreateDatabaseAsync(identityDatabase);

        EventConnectionString = BuildConnectionString(eventDatabase);
        IdentityConnectionString = BuildConnectionString(identityDatabase);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private async Task CreateDatabaseAsync(string databaseName)
    {
        await using var connection = new SqlConnection(BuildConnectionString("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
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