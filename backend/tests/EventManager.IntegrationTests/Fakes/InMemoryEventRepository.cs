using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;

namespace EventManager.IntegrationTests.Fakes;

/// <summary>
/// In-memory implementation of IEventRepository for integration tests.
/// No database required — data lives in a simple dictionary.
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private readonly Dictionary<Guid, Event> _store = [];

    /// <summary>Seeds an event directly into the store, bypassing business logic.</summary>
    public void Seed(Event @event) => _store[@event.Id] = @event;

    public Task<IEnumerable<Event>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var result = _store.Values
            .Where(e => e.Date >= DateTime.UtcNow)
            .OrderBy(e => e.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Task.FromResult(result);
    }

    public Task<Event?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var @event);
        return Task.FromResult(@event);
    }

    public Task<Guid> CreateAsync(Event @event)
    {
        var id = Guid.NewGuid();
        @event.Id = id;
        @event.CreatedAt = DateTime.UtcNow;
        _store[id] = @event;
        return Task.FromResult(id);
    }

    public Task<bool> UpdateAsync(Event @event)
    {
        if (!_store.ContainsKey(@event.Id)) return Task.FromResult(false);
        _store[@event.Id] = @event;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var removed = _store.Remove(id);
        return Task.FromResult(removed);
    }
}
