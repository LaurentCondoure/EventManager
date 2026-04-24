using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;


namespace EventManager.Domain.Services;

/// <summary>Implements business logic for event management.</summary>
public class EventService(
    IEventRepository eventRepository) : IEventService
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
            Capacity    = request.Capacity,
            Price       = request.Price,
            Category    = request.Category,
            CreatedAt   = DateTime.UtcNow
        };

        var id = await _eventRepository.CreateAsync(@event);
        @event.Id = id;

        return MapToDto(@event);
    }

    /// <summary>Maps a domain <see cref="Event"/> to its read DTO.</summary>
    private static EventDto MapToDto(Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.Date,
        e.Capacity,
        e.Price,
        e.Category,
        e.CreatedAt,
        e.UpdatedAt
    );
}
