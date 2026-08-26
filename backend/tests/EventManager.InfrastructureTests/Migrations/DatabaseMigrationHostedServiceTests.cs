using EventManager.Infrastructure.Identity;
using EventManager.Infrastructure.Migrations;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Events;
using EventManager.InfrastructureTests.Fixtures;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventManager.InfrastructureTests.Migrations;

public sealed class DatabaseMigrationHostedServiceTests : IClassFixture<MigrationSqlServerFixture>
{
    private readonly MigrationSqlServerFixture _fixture;

    public DatabaseMigrationHostedServiceTests(MigrationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartAsync_AppliesEventAndIdentityMigrations()
    {
        var service = CreateService();

        await service.StartAsync(TestContext.Current.CancellationToken);

        await using var eventContext = CreateEventContext();
        await using var identityContext = CreateIdentityContext();

        (await eventContext.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await identityContext.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await eventContext.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().ContainSingle("20260826075538_InitialEvents");
        (await identityContext.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().ContainSingle("20260826073900_InitialIdentity");

        (await eventContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = 'Events'")
            .SingleAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await identityContext.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN ('AspNetUsers', 'AspNetRoles', 'RefreshTokens', 'PasswordHistory')")
            .SingleAsync(TestContext.Current.CancellationToken)).Should().Be(4);
    }

    [Fact]
    public async Task StartAsync_CanBeCalledTwiceWithoutPendingMigrations()
    {
        var service = CreateService();

        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);

        await using var eventContext = CreateEventContext();
        await using var identityContext = CreateIdentityContext();

        (await eventContext.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await identityContext.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    private DatabaseMigrationHostedService CreateService()
    {
        var options = Options.Create(new DatabaseOptions
        {
            DefaultMigrationConnection = _fixture.EventConnectionString,
            IdentityMigrationConnection = _fixture.IdentityConnectionString
        });

        return new DatabaseMigrationHostedService(options, NullLogger<DatabaseMigrationHostedService>.Instance);
    }

    private EventManagerDbContext CreateEventContext()
    {
        var options = new DbContextOptionsBuilder<EventManagerDbContext>()
            .UseSqlServer(_fixture.EventConnectionString)
            .Options;
        return new EventManagerDbContext(options);
    }

    private EventManagerIdentityDbContext CreateIdentityContext()
    {
        var options = new DbContextOptionsBuilder<EventManagerIdentityDbContext>()
            .UseSqlServer(_fixture.IdentityConnectionString)
            .Options;
        return new EventManagerIdentityDbContext(options);
    }
}