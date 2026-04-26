using EventManager.Domain.Entities;
using EventManager.Domain.Interfaces;
using EventManager.Infrastructure.Options;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace EventManager.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of <see cref="ICommentRepository"/>.
/// </summary>
public class MongoDbCommentRepository(IMongoClient client, IOptions<MongoDbOptions> options) : ICommentRepository
{
    /// <summary>
    /// Provides access to the MongoDB 'event_comments' collection used to store and retrieve event comments.
    /// </summary>
    private readonly IMongoCollection<EventComment> _collection =
        client.GetDatabase(options.Value.DatabaseName).GetCollection<EventComment>("event_comments");

    /// <inheritdoc/>
    public async Task<IEnumerable<EventComment>> GetByEventIdAsync(Guid eventId)
    {
        var filter = Builders<EventComment>.Filter.Eq(c => c.EventId, eventId);
        var sort   = Builders<EventComment>.Sort.Descending(c => c.CreatedAt);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<string> CreateAsync(EventComment comment)
    {
        comment.CreatedAt = DateTime.UtcNow;

        await _collection.InsertOneAsync(comment);

        return comment.Id;
    }
}
