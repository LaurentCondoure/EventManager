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
    /// <summary>
    /// Repository that performs data access operations.
    /// </summary>
    private readonly IEventRepository _inner = inner;
    
    /// <summary>
    /// Redis database instance for cache operations.
    /// </summary>
    private readonly IDatabase _cache = redis.GetDatabase();

    /// <summary>
    /// Default time to live for cached items.
    /// </summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Key to track the version of the events list. 
    /// Incrementing this value on writes allows us to invalidate all paginated list entries without having to track them individually.
    /// </summary>
    private const string ListVersionKey = "events:list:version";

    // ── cacheKeys ────────────────────────────────────────────────────────
    /// <summary>
    /// Get the single event chache key for specified eventId
    /// </summary>
    /// <param name="id">Unique id of the event</param>
    /// <returns>Key to retrieve the event data from cache</returns>
    private static string EventKey(Guid id)            => $"event:{id}";

    /// <summary>
    /// Get the cache key for a paginated list of events, 
    /// including the version to allow for invalidation on writes.    
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="size">Number of items per page</param>
    /// <returns>string containing the cache key to retrieve the paginated list from cache</returns>
        private static string PageKey(int page, int size, long version) 
        => $"events:page:{page}:size:{size}:v{version}";
    // ── Read ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IEnumerable<Event>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        long version = (long?)await _cache.StringGetAsync(ListVersionKey) ?? 0L;
        string key = PageKey(page, pageSize, version);
        RedisValue cached = await _cache.StringGetAsync(key);

        if (cached.HasValue)
            return JsonSerializer.Deserialize<IEnumerable<Event>>(cached!)!;

        IEnumerable<Event> events = await _inner.GetAllAsync(page, pageSize);
        await _cache.StringSetAsync(key, JsonSerializer.Serialize(events), Ttl);

        return events;
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id)
    {
        string key = EventKey(id);
        RedisValue cached = await _cache.StringGetAsync(key);

        if (cached.HasValue)
            return JsonSerializer.Deserialize<Event>(cached!);

        Event? @event = await _inner.GetByIdAsync(id);

        if (@event is not null)
            await _cache.StringSetAsync(key, JsonSerializer.Serialize(@event), Ttl);

        return @event;
    }

    // ── Write — delegates then Invalidates cache ────────────────────────────

    /// <inheritdoc/>
    public async Task<Guid> CreateAsync(Event @event)
    {
        var id = await _inner.CreateAsync(@event);

        //Incr will set +1 to the version, all oagined list keys will be invalidate 
        //next time they are requested
        await _cache.StringIncrementAsync(ListVersionKey);
        return id;
    }

}
