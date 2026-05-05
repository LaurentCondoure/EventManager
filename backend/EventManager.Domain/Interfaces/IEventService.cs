using EventManager.Domain.DTOs;

namespace EventManager.Domain.Interfaces;

/// <summary>Business logic contract for event management.</summary>
public interface IEventService
{
    /// <summary>
    /// Returns upcoming events ordered by date ascending, with pagination.
    /// </summary>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Number of events per page.</param>
    /// <returns>List of <see cref="EventDto"/> matching the requested page</returns>
    Task<IEnumerable<EventDto>> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>Returns a single event by its ID.</summary>
    /// <param name="id">The event ID.</param>
    /// <exception cref="EventManager.Domain.Exceptions.NotFoundException">Thrown when the event does not exist.</exception>
    Task<EventDto> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new event and returns its full representation.
    /// </summary>
    /// <param name="request">The creation payload.</param>
    Task<EventDto> CreateAsync(CreateEventInput
        request);

    /// <summary>
    /// Searches events by full-text query across title, description, category and artist name.
    /// </summary>
    /// <param name="query">Search query string.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Number of events per page.</param>
    /// <returns>List of <see cref="SearchResultDto"/> matching the search query and requested page.</returns>
    Task<IEnumerable<SearchResultDto>> SearchAsync(string query, int page = 1, int pageSize = 20);

    /// <summary>
    /// Returns all comments for a given event, ordered by creation date descending.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <exception cref="EventManager.Domain.Exceptions.NotFoundException">Thrown when the event does not exist.</exception>
    Task<IEnumerable<CommentDto>> GetCommentsAsync(Guid eventId);

    /// <summary>Adds a comment to an existing event.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="input">The comment creation payload.</param>
    /// <exception cref="EventManager.Domain.Exceptions.NotFoundException">Thrown when the event does not exist.</exception>
    Task<CommentDto> AddCommentAsync(Guid eventId, CreateCommentInput input);

    /// <summary>Returns an event combined with its comments in a single call.</summary>
    /// <param name="id">The event ID.</param>
    /// <exception cref="EventManager.Domain.Exceptions.NotFoundException">Thrown when the event does not exist.</exception>
    Task<EventWithCommentsDto> GetWithCommentsAsync(Guid id);

    /// <summary>Updates an existing event and reindexes it in the search engine.</summary>
    /// <param name="id">The event ID.</param>
    /// <param name="request">The update payload.</param>
    /// <exception cref="EventManager.Domain.Exceptions.NotFoundException">Thrown when the event does not exist.</exception>
    Task<EventDto> UpdateAsync(Guid id, UpdateEventInput request);

    /// <summary>Deletes an event and removes it from the search index.</summary>
    /// <param name="id">The event ID.</param>
    /// <exception cref="EventManager.Domain.Exceptions.NotFoundException">Thrown when the event does not exist.</exception>
    Task DeleteAsync(Guid id);
}
