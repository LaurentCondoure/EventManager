using EventManager.Domain.Entities;
using EventManager.Domain.Interfaces;

namespace EventManager.IntegrationTests.Fakes;

/// <summary>
/// In-memory implementation of ICommentRepository for integration tests.
/// No database required — data lives in a simple list.
/// </summary>
public class InMemoryCommentRepository : ICommentRepository
{
    private readonly List<EventComment> _store = [];

    /// <summary>Seeds a comment directly into the store, bypassing business logic.</summary>
    public void Seed(EventComment comment)
    {
        comment.Id ??= Guid.NewGuid().ToString();
        _store.Add(comment);
    }

    public Task<IEnumerable<EventComment>> GetByEventIdAsync(Guid eventId)
    {
        var result = _store
            .Where(c => c.EventId == eventId)
            .OrderByDescending(c => c.CreatedAt)
            .AsEnumerable();

        return Task.FromResult(result);
    }

    public Task<string> CreateAsync(EventComment comment)
    {
        var id = Guid.NewGuid().ToString();
        comment.Id = id;
        comment.CreatedAt = DateTime.UtcNow;
        _store.Add(comment);
        return Task.FromResult(id);
    }
}
