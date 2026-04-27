using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/comments")]
public class CommentsController(ICommentService commentService, ILogger<CommentsController> logger) : ControllerBase
{

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CommentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(Guid eventId)
    {
        logger.LogInformation("Retrieved comments for event {EventId}", eventId);

        IEnumerable<CommentDto> commentDTOs = await commentService.GetCommentsByEventId(eventId);

        return Ok(commentDTOs);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(Guid eventId, [FromBody] CreateCommentInput request)
    {

        CommentDto commentDTO = await commentService.CreateAsync(eventId, request);

        logger.LogInformation("Created comment {CommentId} for event {EventId}", commentDTO.Id, eventId);

        return CreatedAtAction(nameof(GetByEvent), new { eventId }, commentDTO);
    }
}
