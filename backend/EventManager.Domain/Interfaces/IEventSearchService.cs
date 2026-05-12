using EventManager.Domain.DTOs;
using EventManager.Domain.Entities;

namespace EventManager.Domain.Interfaces;

/// <summary>
/// Logic contract for event search functionality, abstracting the underlying search implementation (e.g., Elasticsearch).
/// </summary>
public interface IEventSearchService
{
    /// <summary>
    /// Indexes an event in search engine for search purposes, allowing it to be retrieved based on search queries.
    /// </summary>
    /// <param name="event"><see cref="Event"/> instance to be indexed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IndexAsync(Event @event);

    /// <summary>
    /// Deletes an event from the search index, ensuring it no longer appears in search results.
    /// </summary>
    /// <param name="eventId">Id of the event to delete</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid eventId);

    /// <summary>
    /// Searches for events based on the provided query string.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A collection of <see cref="SearchResultDto"/> matching the search criteria.</returns>
    Task<IEnumerable<SearchResultDto>> SearchAsync(string query, int page = 1, int pageSize = 20);

    /// <summary>
    /// Rebuilds the Elasticsearch index from SQL Server — use when the two stores diverge. 
    /// </summary>
    /// <param name="events">The collection of events to be indexed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReindexAllAsync(IEnumerable<Event> events);
}
