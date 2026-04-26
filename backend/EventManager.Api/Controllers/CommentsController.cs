using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/comments")]
public class CommentsController(ICommentRepository commentRepository, ILogger<CommentsController> logger) : ControllerBase
{

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CommentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(Guid eventId)
    {
        logger.LogInformation("Retrieved comments for event {EventId}", eventId);

        var comments = await commentRepository.GetByEventIdAsync(eventId);

        

        var dtos = comments.Select(c => new CommentDto(
            c.Id, c.EventId, c.UserId, c.UserName, c.Text, c.Rating, c.CreatedAt));

        return Ok(dtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Guid eventId, [FromBody] CreateCommentInput request)
    {
        var comment = new EventComment
        {
            EventId  = eventId,
            UserId   = request.UserId,
            UserName = request.UserName,
            Text     = request.Text,
            Rating   = request.Rating
        };

        var id = await commentRepository.CreateAsync(comment);

        logger.LogInformation("Created comment {CommentId} for event {EventId}", id, eventId);

        var dto = new CommentDto(id, eventId, request.UserId, request.UserName, request.Text, request.Rating, comment.CreatedAt);
        return CreatedAtAction(nameof(GetByEvent), new { eventId }, dto);
    }
}
