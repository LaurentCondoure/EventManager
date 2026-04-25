using EventManager.Domain.Entities;
using EventManager.Domain.Interfaces;
using EventManager.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Moq;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text.Json;

namespace EventManager.UnitTests.Repositories;

/// <summary>
/// Tests for <see cref="CachedEventRepository"/>.
/// Verifies cache-aside behaviour, versioned key invalidation, and delegation to the inner repository.
/// </summary>
public class CachedEventRepositoryTests
{
    /// <summary>
    /// Mock of the inner repository (SQL Server layer).
    /// </summary>
    private readonly Mock<IEventRepository> _innerMock = new();

    /// <summary>
    /// Mock of the Redis IDatabase used for all cache operations.
    /// </summary>
    private readonly Mock<IDatabase> _dbMock = new();

    /// <summary>
    /// System Under Test: CachedEventRepository with mocked inner repository and Redis database.
    /// </summary>
    private readonly CachedEventRepository _sut;

    public CachedEventRepositoryTests()
    {
        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                 .Returns(_dbMock.Object);

        _sut = new CachedEventRepository(_innerMock.Object, redisMock.Object);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_CacheMiss_CallsInnerAndStoresResultWithVersionedKey()
    {
        // Arrange
        var events = new List<Event> { BuildEvent() };
        SetupVersionKey(RedisValue.Null);         // version absent → defaults to 0
        SetupPageKey(RedisValue.Null);            // page key miss
        _innerMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync(events);
        SetupStringSet();

        // Act
        var result = await _sut.GetAllAsync();
            
        // Assert
        _innerMock.Verify(r => r.GetAllAsync(1, 20), Times.Once);
        _dbMock.Verify(d => d.StringSetAsync(
                                            It.Is<RedisKey>(
                                                k => ((string)k!).Contains(":v0")),
                                                It.IsAny<RedisValue>(),
                                                It.IsAny<Expiration>(),
                                                It.IsAny<ValueCondition>(),
                                                It.IsAny<CommandFlags>() )
                                            , Times.Once);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_CacheHit_ReturnsFromCacheWithoutCallingInner()
    {
        // Arrange
        var events = new List<Event> { BuildEvent(), BuildEvent() };
        SetupVersionKey(2L);
        SetupPageKey((RedisValue)JsonSerializer.Serialize(events));

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        _innerMock.Verify(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_UsesCurrentVersionInCacheKey()
    {
        // Arrange
        SetupVersionKey(5L);
        SetupPageKey(RedisValue.Null);
        _innerMock.Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync([]);
        SetupStringSet();

        // Act
        await _sut.GetAllAsync();

        // Assert — the page lookup must use the version returned by Redis
        _dbMock.Verify(d => d.StringGetAsync(
            It.Is<RedisKey>(k => ((string)k!).Contains(":v5")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsFromCacheWithoutCallingInner()
    {
        // Arrange
        var @event = BuildEvent();
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync((RedisValue)JsonSerializer.Serialize(@event));

        // Act
        var result = await _sut.GetByIdAsync(@event.Id);

        // Assert
        _innerMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        result.Should().NotBeNull();
        result!.Id.Should().Be(@event.Id);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_CallsInnerAndStoresResult()
    {
        // Arrange
        var @event = BuildEvent();
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);
        _innerMock.Setup(r => r.GetByIdAsync(@event.Id)).ReturnsAsync(@event);
        SetupStringSet();

        // Act
        var result = await _sut.GetByIdAsync(@event.Id);

        result.Should().NotBeNull();

        // Assert
        _innerMock.Verify(r => r.GetByIdAsync(@event.Id), Times.Once);
        _dbMock.Verify(d => d.StringSetAsync(
                                            It.Is<RedisKey>(
                                                k => ((string)k!).Contains(@event.Id.ToString())),
                                                It.IsAny<RedisValue>(),
                                                It.IsAny<Expiration>(),
                                                It.IsAny<ValueCondition>(),
                                                It.IsAny<CommandFlags>() )
                                            , Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_NullFromInner_DoesNotStoreInCache()
    {
        // Arrange
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);
        _innerMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Event?)null);

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        _dbMock.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
        result.Should().BeNull();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DelegatesToInnerAndReturnsId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        _innerMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(expectedId);
        SetupStringIncrement();

        // Act
        var result = await _sut.CreateAsync(BuildEvent());

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateAsync_IncrementsListVersionKey()
    {
        // Arrange
        _innerMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(Guid.NewGuid());
        SetupStringIncrement();

        // Act
        await _sut.CreateAsync(BuildEvent());

        // Assert
        _dbMock.Verify(d => d.StringIncrementAsync(
            It.Is<RedisKey>(k => k == "events:list:version"),
            It.IsAny<long>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Event BuildEvent() => new()
    {
        Id          = Guid.NewGuid(),
        Title       = "Test Concert",
        Description = "A test event description",
        Date        = DateTime.UtcNow.AddDays(30),
        Capacity    = 100,
        Price       = 25m,
        Category    = "Concert",
        CreatedAt   = DateTime.UtcNow
    };

    private void SetupVersionKey(RedisValue value) =>
        _dbMock.Setup(d => d.StringGetAsync(
            It.Is<RedisKey>(k => k == "events:list:version"),
            It.IsAny<CommandFlags>()))
        .ReturnsAsync(value);

    private void SetupPageKey(RedisValue value) =>
        _dbMock.Setup(d => d.StringGetAsync(
            It.Is<RedisKey>(k => ((string?)k)!.StartsWith("events:page:")),
            It.IsAny<CommandFlags>()))
        .ReturnsAsync(value);

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// StackExchange.Redis 2.12.x introduced a new priority overload:     /// it is the one that the compiler chooses for StringSetAsync(key, value, Ttl)
    /// Task<bool> StringSetAsync(RedisKey key, RedisValue value, Expiration expiry = default, ValueCondition when = default, CommandFlags flags = CommandFlags.None);
    /// TimeSpan implicitly converts to Expiration.The old overloads with TimeSpan? no longer have default values — so they can no longer be resolved with 3 arguments.
    /// </remarks>
    private void SetupStringSet() =>
        _dbMock.Setup(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>(),       // nouveau type
            It.IsAny<ValueCondition>(),   // nouveau type
            It.IsAny<CommandFlags>()))
        .ReturnsAsync(true);

    private void SetupStringIncrement() =>
        _dbMock.Setup(d => d.StringIncrementAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<long>(),
            It.IsAny<CommandFlags>()))
        .ReturnsAsync(1L);
}