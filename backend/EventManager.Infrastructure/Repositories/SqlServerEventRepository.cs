using EventManager.Domain.Events.Entities;
using EventManager.Domain.Events.Interfaces;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Queries;

using System.Data;

using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;


namespace EventManager.Infrastructure.Repositories;

/// <summary>Dapper-based implementation of <see cref="IEventRepository"/> targeting SQL Server.</summary>
/// <remarks>
/// This repository uses raw SQL queries defined in <see cref="EventQueries"/> for better performance and maintainability.
/// </remarks>
public class SqlServerEventRepository(IOptions<DatabaseOptions> options) : IEventRepository
{
    private IDbConnection CreateConnection() => new SqlConnection(options.Value.DefaultConnection);

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
            @event.Location,
            @event.Capacity,
            @event.Price,
            @event.Category,
            @event.ArtistName,
            CreatedAt   = DateTime.UtcNow
        });

        return id;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Event @event)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(EventQueries.Update, new
        {
            @event.Id,
            @event.Title,
            @event.Description,
            @event.Date,
            @event.Location,
            @event.Capacity,
            @event.Price,
            @event.Category,
            @event.ArtistName,
            @event.UpdatedAt
        });
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(EventQueries.Delete, new { Id = id });
    }
}
