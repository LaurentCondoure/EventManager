using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/comments")]
public class CommentsController(IEventService eventService, ILogger<CommentsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(Guid eventId)
    {
        logger.LogInformation("Retrieved comments for event {EventId}", eventId);
        return Ok(await eventService.GetCommentsAsync(eventId));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid eventId, [FromBody] CreateCommentInput request)
    {
        CommentDto dto = await eventService.AddCommentAsync(eventId, request);
        logger.LogInformation("Created comment {CommentId} for event {EventId}", dto.Id, eventId);
        return CreatedAtAction(nameof(GetByEvent), new { eventId }, dto);
    }
}
