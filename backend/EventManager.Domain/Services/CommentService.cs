using EventManager.Domain.DTOs;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;


namespace EventManager.Domain.Services
{
    /// <inheritdoc/>
    public class CommentService(ICommentRepository commentRepository) : ICommentService
    {
        /// <inheritdoc/>
        public async Task<CommentDto> CreateAsync(Guid eventId, CreateCommentInput createCommentInput)
        {
            EventComment comment = new EventComment
            {
                EventId = eventId,
                UserId = createCommentInput.UserId,
                UserName = createCommentInput.UserName,
                Text = createCommentInput.Text,
                Rating = createCommentInput.Rating,
                CreatedAt = DateTime.UtcNow
            };

            string id = await commentRepository.CreateAsync(comment);

            return new CommentDto(id, eventId, comment.UserId, comment.UserName, comment.Text, comment.Rating, comment.CreatedAt);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CommentDto>> GetCommentsByEventId(Guid eventId)
        {
            IEnumerable<EventComment> comments = await commentRepository.GetByEventIdAsync(eventId);

            return comments.Select(c => new CommentDto(
                c.Id, c.EventId, c.UserId, c.UserName, c.Text, c.Rating, c.CreatedAt));
        }
    }
}
