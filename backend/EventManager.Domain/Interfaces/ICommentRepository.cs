using EventManager.Domain.Entities;

namespace EventManager.Domain.Interfaces;

/// <summary>
/// Data access contract for event comments (MongoDB).
/// </summary>
public interface ICommentRepository
{
    /// <summary>
    /// Returns all comments for a given event, ordered by creation date descending.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    Task<IEnumerable<EventComment>> GetByEventIdAsync(Guid eventId);

    /// <summary>Inserts a new comment and returns its generated ObjectId.</summary>
    /// <param name="comment">The comment to insert.</param>
    Task<string> CreateAsync(EventComment comment);
}
