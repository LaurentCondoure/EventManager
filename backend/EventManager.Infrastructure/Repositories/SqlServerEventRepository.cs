using EventManager.Domain.Entities;
using EventManager.Domain.Interfaces;
using EventManager.Infrastructure.Factories;
using EventManager.Infrastructure.Queries;

using System.Data;

using Dapper;


namespace EventManager.Infrastructure.Repositories;

/// <summary>Dapper-based implementation of <see cref="IEventRepository"/> targeting SQL Server.</summary>
/// <remarks>
/// This repository uses raw SQL queries defined in <see cref="EventQueries"/> for better performance and maintainability.
/// </remarks>
/// <param name="options">Database connection options injected via IOptions pattern.</param>
public class SqlServerEventRepository(IDbConnectionFactory dbConnectionfactory) : IEventRepository
{
    private readonly IDbConnectionFactory _dbConnectionfactory = dbConnectionfactory;

    /// <summary>Creates and returns a new DB connection using the configured factory.</summary>
    private IDbConnection CreateConnection() => _dbConnectionfactory.CreateConnection();

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
}
