using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;

using Microsoft.Extensions.Logging;


namespace EventManager.Domain.Services;

/// <summary>Implements business logic for event management.</summary>
public class EventService(
    IEventRepository eventRepository,
    ICommentRepository commentRepository,
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
    public async Task<IEnumerable<SearchResultDto>> SearchAsync(string query, int page = 1, int pageSize = 20)
        => await searchService.SearchAsync(query, page, pageSize);

    /// <inheritdoc/>
    public async Task<IEnumerable<CommentDto>> GetCommentsAsync(Guid eventId)
    {
        _ = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new NotFoundException(nameof(Event), eventId);

        var comments = await commentRepository.GetByEventIdAsync(eventId);
        return comments.Select(MapToCommentDto);
    }

    /// <inheritdoc/>
    public async Task<CommentDto> AddCommentAsync(Guid eventId, CreateCommentInput input)
    {
        _ = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new NotFoundException(nameof(Event), eventId);

        var comment = new EventComment
        {
            EventId   = eventId,
            UserId    = input.UserId,
            UserName  = input.UserName,
            Text      = input.Text,
            Rating    = input.Rating,
            CreatedAt = DateTime.UtcNow
        };

        var id = await commentRepository.CreateAsync(comment);
        comment.Id = id;

        return MapToCommentDto(comment);
    }

    /// <inheritdoc/>
    public async Task<EventDto> UpdateAsync(Guid id, UpdateEventInput request)
    {
        var @event = await _eventRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Event), id);

        @event.Title       = request.Title;
        @event.Description = request.Description;
        @event.Date        = request.Date;
        @event.Location    = request.Location;
        @event.Capacity    = request.Capacity;
        @event.Price       = request.Price;
        @event.Category    = request.Category;
        @event.ArtistName  = request.ArtistName;
        @event.UpdatedAt   = DateTime.UtcNow;

        await _eventRepository.UpdateAsync(@event);
        await TryIndexAsync(@event);

        return MapToDto(@event);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        _ = await _eventRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Event), id);

        await _eventRepository.DeleteAsync(id);
        await TryDeleteFromSearchAsync(id);
    }

    /// <inheritdoc/>
    public async Task<EventWithCommentsDto> GetWithCommentsAsync(Guid id)
    {
        var @event = await _eventRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Event), id);

        var comments = await commentRepository.GetByEventIdAsync(id);

        return new EventWithCommentsDto(MapToDto(@event), comments.Select(MapToCommentDto));
    }

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

    private static CommentDto MapToCommentDto(EventComment c) => new(
        c.Id, c.EventId, c.UserId, c.UserName, c.Text, c.Rating, c.CreatedAt);

    /// <summary>Indexes the event in Elasticsearch. Logs and swallows any failure so a search outage never blocks event creation.</summary>
    private async Task TryIndexAsync(Event @event)
    {
        try { await searchService.IndexAsync(@event); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search indexing failed for event {EventId} — search index may be stale", @event.Id);
        }
    }

    /// <summary>Removes the event from Elasticsearch. Logs and swallows any failure so a search outage never blocks deletion.</summary>
    private async Task TryDeleteFromSearchAsync(Guid id)
    {
        try { await searchService.DeleteAsync(id); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search deletion failed for event {EventId} — search index may be stale", id);
        }
    }
}
