using EventManagement.Domain.Entities;
using EventManagement.Domain.DTOs;

namespace EventManagement.Domain.Interfaces;

/// <summary>Data access contract for the Events table.</summary>
public interface IEventRepository
{
    /// <summary>
    /// Returns upcoming events (Date >= today) ordered by date ascending, with pagination.
    /// </summary>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Number of events per page.</param>
    Task<IEnumerable<Event>> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>Returns a single event by its ID, or null if not found.</summary>
    /// <param name="id">The event ID.</param>
    Task<Event?> GetByIdAsync(Guid id);

    /// <summary>Inserts a new event and returns its generated ID.</summary>
    /// <param name="event">The event to insert.</param>
    Task<Guid> CreateAsync(Event @event);
}
