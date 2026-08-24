using EventManager.Domain.Events.DTOs;
using EventManager.Domain.Events.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("fixed")]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        IEnumerable<EventDto> events = await eventService.GetAllAsync(page, pageSize);
        //Log after service call to ensure any errors are not misinterpreted after retrieval confirmation
        logger.LogInformation("Retrieved events page {Page} with page size {PageSize}", page, pageSize);
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        EventDto @event = await eventService.GetByIdAsync(id);
        //Log after service call to ensure any errors are not misinterpreted after retrieval confirmation
        logger.LogInformation("Retrieved event for {EventId}", id);
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

    [HttpGet("{id:guid}/full")]
    [ProducesResponseType(typeof(EventWithCommentsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFull(Guid id)
    {
        EventWithCommentsDto @event = await eventService.GetWithCommentsAsync(id);
        logger.LogInformation("Retrieved full event for {EventId}", id);
        return Ok(@event);
    }
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventInput request)
    {
        EventDto @event = await eventService.UpdateAsync(id, request);
        logger.LogInformation("Updated event {EventId} - {Title}", @event.Id, @event.Title);
        return Ok(@event);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await eventService.DeleteAsync(id);
        logger.LogInformation("Deleted event {EventId}", id);
        return NoContent();
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<SearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        IEnumerable<SearchResultDto> results = await eventService.SearchAsync(q, page, pageSize);
        logger.LogInformation("Search events with {searchQuery} page {Page} with page size {PageSize}", q, page, pageSize);
        return Ok(results);
    }
}
