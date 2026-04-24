using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{
    private readonly IEventService _eventService = eventService;
    private readonly ILogger<EventsController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var events = await _eventService.GetAllAsync(page, pageSize);
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var @event = await _eventService.GetByIdAsync(id);
        Response.Headers.CacheControl = "public, max-age=600";
        return Ok(@event);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEventInput request)
    {
        var @event = await _eventService.CreateAsync(request);
        _logger.LogInformation("Created event {EventId} - {Title}", @event.Id, @event.Title);
        return CreatedAtAction(nameof(GetById), new { id = @event.Id }, @event);
    }
}
