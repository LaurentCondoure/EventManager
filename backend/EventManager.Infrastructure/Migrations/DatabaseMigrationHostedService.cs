using EventManager.Infrastructure.Events;
using EventManager.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventManager.Infrastructure.Options;

namespace EventManager.Infrastructure.Migrations;

public sealed class DatabaseMigrationHostedService(
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        DatabaseOptions options = databaseOptions.Value;

        var eventContextOptions = new DbContextOptionsBuilder<EventManagerDbContext>()
            .UseSqlServer(options.DefaultMigrationConnection)
            .Options;
        await using EventManagerDbContext eventContext = new(eventContextOptions);
        await ApplyMigrationAsync(eventContext, "EventManager", cancellationToken);

        var identityContextOptions = new DbContextOptionsBuilder<EventManagerIdentityDbContext>()
            .UseSqlServer(options.IdentityMigrationConnection)
            .Options;
        await using EventManagerIdentityDbContext identityContext = new(identityContextOptions);
        await ApplyMigrationAsync(identityContext, "EventManager_Identity", cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ApplyMigrationAsync(
        DbContext context,
        string databaseName,
        CancellationToken cancellationToken)
    {
        string[] pendingMigrations = (await context.Database
            .GetPendingMigrationsAsync(cancellationToken))
            .ToArray();

        if (pendingMigrations.Length == 0)
        {
            logger.LogInformation("EF Core migrations: {Database} is already up to date", databaseName);
            return;
        }

        logger.LogInformation(
            "EF Core migrations: applying {Count} migration(s) to {Database}: {Migrations}",
            pendingMigrations.Length,
            databaseName,
            string.Join(", ", pendingMigrations));

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("EF Core migrations: {Database} applied successfully", databaseName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "EF Core migrations: failed for {Database}", databaseName);
            throw;
        }
    }
}