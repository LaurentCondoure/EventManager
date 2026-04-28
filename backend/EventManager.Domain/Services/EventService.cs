using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;

using Microsoft.Extensions.Logging;


namespace EventManager.Domain.Services;

/// <summary>Implements business logic for event management.</summary>
public class EventService(
    IEventRepository eventRepository, 
    IEventSearchService searchService,
    ILogger<EventService> logger) : IEventService
{
    private readonly IEventRepository _eventRepository = eventRepository;

    /// <inheritdoc/>
    public async Task<IEnumerable<EventDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var events = await _eventRepository.GetAllAsync(page, pageSize);
        return events.Select(MapToDto);
    }

    /// <inheritdoc/>
    public async Task<EventDto> GetByIdAsync(Guid id)
    {
        var @event = await _eventRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Event), id);

        return MapToDto(@event);
    }

    /// <inheritdoc/>
    public async Task<EventDto> CreateAsync(CreateEventInput request)
    {
        var @event = new Event
        {
            Title       = request.Title,
            Description = request.Description,
            Date        = request.Date,
            Location    = request.Location,
            Capacity    = request.Capacity,
            Price       = request.Price,
            Category    = request.Category,
            ArtistName  = request.ArtistName,
            CreatedAt   = DateTime.UtcNow
        };

        var id = await _eventRepository.CreateAsync(@event);
        @event.Id = id;

        await TryIndexAsync(@event);

        return MapToDto(@event);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventDto>> SearchAsync(string query, int page = 1, int pageSize = 20)
        => await searchService.SearchAsync(query, page, pageSize);

    /// <summary>Maps a domain <see cref="Event"/> to its read DTO.</summary>
    private static EventDto MapToDto(Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.Date,
        e.Location,
        e.Capacity,
        e.Price,
        e.Category,
        e.ArtistName,
        e.CreatedAt,
        e.UpdatedAt
    );

    /// <summary>Indexes the event in Elasticsearch. Logs and swallows any failure so a search outage never blocks event creation.</summary>
    private async Task TryIndexAsync(Event @event)
    {
        try { await searchService.IndexAsync(@event); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search indexing failed for event {EventId} — search index may be stale", @event.Id);
        }
    }

}
