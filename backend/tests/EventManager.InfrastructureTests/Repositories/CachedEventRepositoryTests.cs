using EventManager.Domain.Entities;
using EventManager.Infrastructure.Factories;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using EventManager.InfrastructureTests.Fixtures;

using FluentAssertions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventManager.InfrastructureTests.Repositories;

/// <summary>
/// Infrastructure tests for CachedEventRepository.
/// Verifies the cache-aside pattern with a real Redis instance (Testcontainers).
/// SQL Server is also real — the cache sits in front of actual database reads.
/// </summary>
public class CachedEventRepositoryTests : IClassFixture<SqlServerFixture>, IClassFixture<RedisFixture>
{
    private readonly CachedEventRepository   _sut;
    private readonly SqlServerEventRepository _inner;
    private readonly IDatabase _redis;

    public CachedEventRepositoryTests(SqlServerFixture sqlFixture, RedisFixture redisFixture)
    {
        var connectionFactory = new DbConnectionFactory(
            Options.Create(new DatabaseOptions { DefaultConnection = sqlFixture.ConnectionString }));

        _inner = new SqlServerEventRepository(connectionFactory);

        var redisOptions = Options.Create(new RedisOptions
        {
            ConnectionString = string.Empty,
            TimeToLive       = 1
        });

        _sut   = new CachedEventRepository(_inner, redisFixture.Connection, redisOptions);
        _redis = redisFixture.Connection.GetDatabase();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_CacheMiss_StoresValueInRedis()
    {
        var id = await _inner.CreateAsync(BuildEvent("Cache Miss Concert"));

        await _sut.GetByIdAsync(id);

        var cached = await _redis.StringGetAsync($"event:{id}");
        cached.HasValue.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsSameEvent()
    {
        var id = await _inner.CreateAsync(BuildEvent("Redis Concert"));

        var first  = await _sut.GetByIdAsync(id);
        var second = await _sut.GetByIdAsync(id);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.Id.Should().Be(first!.Id);
        second.Title.Should().Be("Redis Concert");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNullAndDoesNotCache()
    {
        var unknownId = Guid.NewGuid();

        var result = await _sut.GetByIdAsync(unknownId);

        result.Should().BeNull();
        var cached = await _redis.StringGetAsync($"event:{unknownId}");
        cached.HasValue.Should().BeFalse();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_CacheMiss_StoresVersionedKeyInRedis()
    {
        await _inner.CreateAsync(BuildEvent("Versioned Cache Event"));

        var versionBefore = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;

        await _sut.GetAllAsync();

        var pageKey = $"events:page:1:size:20:v{versionBefore}";
        var cached  = await _redis.StringGetAsync(pageKey);
        cached.HasValue.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_CacheHit_ReturnsSameEvents()
    {
        await _inner.CreateAsync(BuildEvent("Cached List Event"));

        var first  = (await _sut.GetAllAsync()).ToList();
        var second = (await _sut.GetAllAsync()).ToList();

        second.Should().HaveCount(first.Count);
        second.Select(e => e.Id).Should().BeEquivalentTo(first.Select(e => e.Id));
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_IncrementsListVersion()
    {
        var versionBefore = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;

        await _sut.CreateAsync(BuildEvent("Version Increment Event"));

        var versionAfter = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;
        versionAfter.Should().Be(versionBefore + 1);
    }

    [Fact]
    public async Task CreateAsync_NewGetAllCall_FetchesFreshDataWithNewVersion()
    {
        var versionBefore = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;
        var title = $"Fresh Event {Guid.NewGuid()}";

        await _sut.CreateAsync(BuildEvent(title));

        var versionAfter = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;
        versionAfter.Should().Be(versionBefore + 1);

        var results = await _sut.GetAllAsync();
        results.Should().Contain(e => e.Title == title);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNonEmptyGuid()
    {
        var id = await _sut.CreateAsync(BuildEvent("Guid Check Event"));

        id.Should().NotBe(Guid.Empty);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_InvalidatesEventCacheKey()
    {
        var id = await _sut.CreateAsync(BuildEvent("Before Update"));
        await _sut.GetByIdAsync(id); // prime cache

        var @event = (await _inner.GetByIdAsync(id))!;
        @event.Title     = "After Update";
        @event.UpdatedAt = DateTime.UtcNow;
        await _sut.UpdateAsync(@event);

        var cached = await _redis.StringGetAsync($"event:{id}");
        cached.IsNull.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_IncrementsListVersion()
    {
        var versionBefore = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;

        var id = await _sut.CreateAsync(BuildEvent("Version Test"));
        var @event = (await _inner.GetByIdAsync(id))!;
        @event.UpdatedAt = DateTime.UtcNow;
        await _sut.UpdateAsync(@event);

        var versionAfter = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;
        versionAfter.Should().BeGreaterThan(versionBefore);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_InvalidatesEventCacheKey()
    {
        var id = await _sut.CreateAsync(BuildEvent("To Delete"));
        await _sut.GetByIdAsync(id); // prime cache

        await _sut.DeleteAsync(id);

        var cached = await _redis.StringGetAsync($"event:{id}");
        cached.IsNull.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_IncrementsListVersion()
    {
        var id = await _sut.CreateAsync(BuildEvent("Delete Version"));
        var versionBefore = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;

        await _sut.DeleteAsync(id);

        var versionAfter = (long?)await _redis.StringGetAsync("events:list:version") ?? 0L;
        versionAfter.Should().Be(versionBefore + 1);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Event BuildEvent(string title = "Test Event") => new()
    {
        Title       = title,
        Description = "Description de test pour le cache",
        Date        = DateTime.UtcNow.AddDays(30),
        Location    = "Paris, Bercy",
        Capacity    = 100,
        Price       = 20m,
        Category    = "Concert",
        CreatedAt   = DateTime.UtcNow
    };
}
