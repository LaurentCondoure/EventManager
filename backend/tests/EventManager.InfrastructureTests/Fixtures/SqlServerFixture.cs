using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace EventManager.InfrastructureTests.Fixtures;

public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await ApplySchemaAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private async Task ApplySchemaAsync()
    {
        var schema = @"
            CREATE TABLE dbo.Events (
                Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
                Title       NVARCHAR(200)    NOT NULL,
                Description NVARCHAR(2000)   NOT NULL,
                Date        DATETIME2        NOT NULL,
                Location    NVARCHAR(200)    NOT NULL,
                Capacity    INT              NOT NULL,
                Price       DECIMAL(10,2)    NOT NULL,
                Category    NVARCHAR(50)     NOT NULL,
                ArtistName  NVARCHAR(200)    NULL,
                CreatedAt   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                UpdatedAt   DATETIME2        NULL,
                CONSTRAINT CK_Events_Capacity CHECK (Capacity > 0),
                CONSTRAINT CK_Events_Price    CHECK (Price >= 0)
            );

            CREATE TABLE dbo.Reservations (
                Id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
                EventId   UNIQUEIDENTIFIER NOT NULL,
                UserEmail NVARCHAR(256)    NOT NULL,
                SeatCount INT              NOT NULL,
                Status    TINYINT          NOT NULL DEFAULT 1,
                CreatedAt DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                UpdatedAt DATETIME2        NULL,
                CONSTRAINT FK_Reservations_Events FOREIGN KEY (EventId) REFERENCES dbo.Events (Id),
                CONSTRAINT CK_Reservations_SeatCount CHECK (SeatCount > 0),
                CONSTRAINT CK_Reservations_Status    CHECK (Status IN (1, 2))
            );";

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(schema, connection);
        await command.ExecuteNonQueryAsync();
    }
}
