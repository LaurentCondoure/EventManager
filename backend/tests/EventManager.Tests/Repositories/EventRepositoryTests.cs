using EventManager.Domain.Entities;
using EventManager.Infrastructure.Factories;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Data;

namespace EventManager.UnitTests.Repositories;

/// <summary>
/// EventRepository test class
/// </summary>
public class EventRepositoryTests : IAsyncLifetime
{
    //Conection to an in-memory SQLite database, initialized with the schema and data from the provided SQL file before each test,
    //and disposed after all tests are completed to ensure a clean testing environment without affecting the original database file.
    private IDbConnection _keepAliveConnection = null!;

    // The factory to create connections for the repository, configured to use the in-memory SQLite database.
    private IDbConnectionFactory _connectionFactory = null!;

    // Known IDs from the seeded test database
    private static readonly Guid Event1Id = new("00000000-0000-0000-0000-000000000001"); // Les Misérables
    private static readonly Guid Event2Id = new("00000000-0000-0000-0000-000000000002"); // Orchestre National
    private static readonly Guid Event3Id = new("00000000-0000-0000-0000-000000000003"); // Festival Jazz

    /// <summary>
    /// Initialize Ressources (database and connection) before each test
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task InitializeAsync()
    {
        // Path to your existing SQLite file
        string dbFilePath = Path.Combine(AppContext.BaseDirectory, "create-event-manager-db.sql");
        if (!File.Exists(dbFilePath))
            throw new FileNotFoundException("SQLite file not found", dbFilePath);

        // Unique name per instance: each test gets its own isolated in-memory database.
        // A fixed name would allow databases to leak across tests via SQLite shared-cache.
        string dbName = $"EventManagerTestDb_{Guid.NewGuid():N}";
        IOptions<DatabaseOptions> options = Options.Create(new DatabaseOptions
        {
            DefaultConnection = $"Data Source={dbName};Mode=Memory;Cache=Shared;Pooling=False"
        });
        var factory = new DbConnectionFactory(options, DbProvider.InMemorySqlite);
        // Keep one connection open for the lifetime of the test: SQLite destroys an in-memory
        // database when the last connection to it is closed.
        SqliteConnection keepAliveConnection = (SqliteConnection)factory.CreateConnection();
        await keepAliveConnection.OpenAsync();

        // ExecuteNonQueryAsync only runs the first statement of a multi-statement script,
        // so split on ';' and execute each statement individually.
        string script = await File.ReadAllTextAsync(dbFilePath);
        foreach (var statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(statement)) continue;
            using var cmd = keepAliveConnection.CreateCommand();
            cmd.CommandText = statement;
            await cmd.ExecuteNonQueryAsync();
        }



        _keepAliveConnection = keepAliveConnection;
        _connectionFactory = factory;
    }

    /// <summary>
    /// Dispose  ressources after any tests
    /// </summary>
    /// <returns>Completed Task</returns>
    public Task DisposeAsync()
    {
        _keepAliveConnection?.Close();
        _keepAliveConnection?.Dispose();
        return Task.CompletedTask;
    }

    // ─── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMatchingEvent()
    {
        var repository = new SqlServerEventRepository(_connectionFactory);

        var result = await repository.GetByIdAsync(Event1Id);

        Assert.NotNull(result);
        Assert.Equal(Event1Id, result.Id);
        Assert.Equal("Les Misérables", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_MapsAllColumns()
    {
        var repository = new SqlServerEventRepository(_connectionFactory);

        var result = await repository.GetByIdAsync(Event1Id);

        Assert.NotNull(result);
        Assert.Equal("Comédie musicale", result.Category);
        Assert.True(result.Capacity > 0);
        Assert.True(result.Price >= 0);
        Assert.NotEqual(default, result.Date);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var repository = new SqlServerEventRepository(_connectionFactory);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidEvent_ReturnsNonEmptyGuid()
    {
        var repository = new SqlServerEventRepository(_connectionFactory);

        var id = await repository.CreateAsync(BuildTestEvent());

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task CreateAsync_ValidEvent_CanBeRetrievedById()
    {
        var repository = new SqlServerEventRepository(_connectionFactory);
        var @event = BuildTestEvent();

        var id = await repository.CreateAsync(@event);
        var retrieved = await repository.GetByIdAsync(id);

        Assert.NotNull(retrieved);
        Assert.Equal(id, retrieved.Id);
        Assert.Equal(@event.Title, retrieved.Title);
        Assert.Equal(@event.Description, retrieved.Description);
        Assert.Equal(@event.Capacity, retrieved.Capacity);
        Assert.Equal(@event.Price, retrieved.Price);
        Assert.Equal(@event.Category, retrieved.Category);
    }

    [Fact]
    public async Task CreateAsync_TwoEvents_EachReceiveDistinctId()
    {
        var repository = new SqlServerEventRepository(_connectionFactory);

        var id1 = await repository.CreateAsync(BuildTestEvent("Event A"));
        var id2 = await repository.CreateAsync(BuildTestEvent("Event B"));

        Assert.NotEqual(id1, id2);
    }

    // ─── GetAllAsync ─────────────────────────────────────────────────────────
    //EventQueries.GetAll uses SQL Server-specific syntax:
    //   - SYSUTCDATETIME()                          → SQLite: datetime('now')
    //   - OFFSET n ROWS FETCH NEXT n ROWS ONLY      → SQLite: LIMIT n OFFSET n
    // These tests will throw a SqliteException until the queries are made database-agnostic.


    // ─── Helper ──────────────────────────────────────────────────────────────

    private static Event BuildTestEvent(string title = "Test Concert") => new()
    {
        Title       = title,
        Description = "A test event description",
        Date        = DateTime.UtcNow.AddDays(30),
        Capacity    = 100,
        Price       = 25.00m,
        Category    = "Concert",
    };
}
