using EventManager.Domain.DTOs;

namespace EventManager.Domain.Interfaces;

/// <summary>Business logic contract for event management.</summary>
public interface IEventService
{
    /// <summary>Returns upcoming events ordered by date ascending, with pagination.</summary>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Number of events per page.</param>
    Task<IEnumerable<EventDto>> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>Returns a single event by its ID.</summary>
    /// <param name="id">The event ID.</param>
    /// <exception cref="EventManager.Domaine.Exceptions.NotFoundException">Thrown when the event does not exist.</exception>
    Task<EventDto> GetByIdAsync(Guid id);

    /// <summary>Creates a new event and returns its full representation.</summary>
    /// <param name="request">The creation payload.</param>
    Task<EventDto> CreateAsync(CreateEventInput
        request);
}
