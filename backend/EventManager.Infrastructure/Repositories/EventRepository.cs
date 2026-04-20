using Dapper;
using EventManagement.Domain.Entities;
using EventManagement.Domain.DTOs;
using EventManagement.Domain.Interfaces;
using EventManagement.Infrastructure.Options;
using EventManagement.Infrastructure.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EventManagement.Infrastructure.Repositories;

/// <summary>Dapper-based implementation of <see cref="IEventRepository"/> targeting SQL Server.</summary>
/// <remarks>
/// This repository uses raw SQL queries defined in <see cref="EventQueries"/> for better performance and maintainability.
/// </remarks>
/// <param name="options">Database connection options injected via IOptions pattern.</param>
public class EventRepository(IOptions<DatabaseOptions> options) : IEventRepository
{
    private readonly DatabaseOptions _options = options.Value;

    /// <summary>Creates and returns a new SQL connection using the configured connection string.</summary>
    private SqlConnection CreateConnection() => new(_options.DefaultConnection);

    /// <inheritdoc/>
    public async Task<IEnumerable<Event>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<Event>(EventQueries.GetAll, new
        {
            Offset   = (page - 1) * pageSize,
            PageSize = pageSize
        });
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id)
    {

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Event>(EventQueries.GetById, new { Id = id });
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateAsync(Event @event)
    {
        var id = Guid.NewGuid();
        using var connection = CreateConnection();
        await connection.ExecuteAsync(EventQueries.Create, new
        {
            Id          = id,
            @event.Title,
            @event.Description,
            @event.Date,
            @event.Capacity,
            @event.Price,
            @event.Category,
            CreatedAt   = DateTime.UtcNow
        });

        return id;
    }
}
