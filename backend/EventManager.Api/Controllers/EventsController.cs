using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        logger.LogInformation("Retrieved events page {Page} with page size {PageSize}", page, pageSize);
        IEnumerable<EventDto> events = await eventService.GetAllAsync(page, pageSize);
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        logger.LogInformation("Retrieved event for {EventId}", id);
        EventDto @event = await eventService.GetByIdAsync(id);
        Response.Headers.CacheControl = "public, max-age=600";
        return Ok(@event);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEventInput request)
    {
        EventDto @event = await eventService.CreateAsync(request);
        logger.LogInformation("Created event {EventId} - {Title}", @event.Id, @event.Title);
        return CreatedAtAction(nameof(GetById), new { id = @event.Id }, @event);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var results = await eventService.SearchAsync(q, page, pageSize);
        return Ok(results);
    }
}
