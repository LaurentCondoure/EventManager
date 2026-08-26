using EventManager.Domain.Events.Entities;
using EventManager.Domain.Events.Interfaces;
using EventManager.Domain.Exceptions;
using EventManager.Infrastructure.Events;
using EventManager.Infrastructure.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace EventManager.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IEventRepository"/> targeting SQL Server.</summary>
/// <remarks>
/// This repository uses raw SQL queries defined in <see cref="EventQueries"/> for better performance and maintainability.
/// </remarks>
public class SqlServerEventRepository : IEventRepository
{
    private readonly EventManagerDbContext _context;

    public SqlServerEventRepository(EventManagerDbContext context)
    {
        _context = context;
    }

    public SqlServerEventRepository(IOptions<DatabaseOptions> options)
    {
        _context = CreateContext(options.Value.DefaultConnection);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Event>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _context.Events
            .AsNoTracking()
            .Where(eventEntity => eventEntity.Date >= DateTime.UtcNow)
            .OrderBy(eventEntity => eventEntity.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id)
    {

        return await _context.Events.AsNoTracking().SingleOrDefaultAsync(eventEntity => eventEntity.Id == id);
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateAsync(Event @event)
    {
        @event.Id = Guid.NewGuid();
        @event.CreatedAt = DateTime.UtcNow;
        _context.Events.Add(@event);
        await _context.SaveChangesAsync();
        return @event.Id;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Event @event)
    {
        Event? existingEvent = await _context.Events.FindAsync(@event.Id);
        if (existingEvent is null)
            throw new NotFoundException(nameof(Event), @event.Id);

        _context.Entry(existingEvent).CurrentValues.SetValues(@event);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        Event? eventEntity = await _context.Events.FindAsync(id);
        if (eventEntity is null)
            return;

        _context.Events.Remove(eventEntity);
        await _context.SaveChangesAsync();
    }

    private static EventManagerDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<EventManagerDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new EventManagerDbContext(options);
    }
}
