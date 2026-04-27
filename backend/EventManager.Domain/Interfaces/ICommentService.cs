
using EventManager.Domain.DTOs;
using EventManager.Domain.Entities;

namespace EventManager.Domain.Interfaces
{
    /// <summary>
    /// Business logic contract for comment on event management.
    /// </summary>
    public interface ICommentService
    {
        /// <summary>
        /// Returns all comments for a given event, ordered by creation date descending.
        /// </summary>
        /// <param name="eventId">Id of the event to get comments from</param>
        /// <returns>list of all comment <see cref="EventComment"/> for the given event</returns>
        Task<IEnumerable<CommentDto>> GetCommentsByEventId(Guid eventId);

        /// <summary>
        /// Create a new comment on a event
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="createCommentInput"><see cref="CreateCommentInput"/> containing the creation payload</param>
        /// <returns><see cref="EventDto"/> containing the result of creation</returns>
        Task<CommentDto> CreateAsync(Guid eventId, CreateCommentInput createCommentInput);
    }
}
