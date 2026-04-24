using EventManager.Domain.Entities;
using EventManager.Domain.Interfaces;

using System.Text.Json;

using StackExchange.Redis;

namespace EventManager.Infrastructure.Repositories;

/// <summary>
/// Decorator over <see cref="IEventRepository"/> that adds a Redis cache layer.
/// Reads check the cache first (cache-aside pattern); writes invalidate the relevant keys.
/// </summary>
public class CachedEventRepository(IEventRepository inner, IConnectionMultiplexer redis) : IEventRepository
{
    private readonly IEventRepository _inner = inner;
    private readonly IDatabase _cache = redis.GetDatabase();

    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    // ── cacheKeys ────────────────────────────────────────────────────────
    /// <summary>
    /// Get the single event chache key for specified eventId
    /// </summary>
    /// <param name="id">Unique id of the event</param>
    /// <returns>Key to retrieve the event data from cache</returns>
    private static string EventKey(Guid id)            => $"event:{id}";
    /// <summary>
    /// 
    /// </summary>
    /// <param name="page"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    private static string PageKey(int page, int size)  => $"events:page:{page}:size:{size}";

    // ── Read ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IEnumerable<Event>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var key = PageKey(page, pageSize);
        var cached = await _cache.StringGetAsync(key);

        if (cached.HasValue)
            return JsonSerializer.Deserialize<IEnumerable<Event>>(cached!)!;

        var events = await _inner.GetAllAsync(page, pageSize);
        await _cache.StringSetAsync(key, JsonSerializer.Serialize(events), Ttl);

        return events;
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id)
    {
        var key = EventKey(id);
        var cached = await _cache.StringGetAsync(key);

        if (cached.HasValue)
            return JsonSerializer.Deserialize<Event>(cached!);

        var @event = await _inner.GetByIdAsync(id);

        if (@event is not null)
            await _cache.StringSetAsync(key, JsonSerializer.Serialize(@event), Ttl);

        return @event;
    }

    // ── Write — delegates then Invalidates cache ────────────────────────────

    /// <inheritdoc/>
    public async Task<Guid> CreateAsync(Event @event)
    {
        var id = await _inner.CreateAsync(@event);

        //Can be a problem in case of many lists and many event creation,
        // await InvalidateListsAsync();
        return id;
    }

    // ── Invalidation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Remove all paginated list's keys from cache. This is a brute-force approach that ensures consistency at the cost of potentially removing more cache entries than necessary. In a real application, consider implementing a more targeted invalidation strategy.
    /// </summary>
    private async Task InvalidateListsAsync()
    {
        var server = _cache.Multiplexer.GetServers().First();// redis.GetServers().First();
        var keys   = server.KeysAsync(pattern: "events:page:*");

        await foreach (var key in keys)
            await _cache.KeyDeleteAsync(key);
    }
}
