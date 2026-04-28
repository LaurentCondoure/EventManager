using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;

namespace EventManager.Infrastructure.Search;

public class EventSearchService : IEventSearchService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "events";

    public EventSearchService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task IndexAsync(Event @event)
    {
        var document = new EventSearchDocument
        {
            Id          = @event.Id,
            Title       = @event.Title,
            Description = @event.Description,
            Date        = @event.Date,
            Location    = @event.Location,
            Price       = @event.Price,
            Category    = @event.Category,
            ArtistName  = @event.ArtistName
        };

        await _client.IndexAsync(document, i => i.Index(IndexName).Id(@event.Id.ToString()));
    }

    public async Task DeleteAsync(Guid eventId)
    {
        await _client.DeleteAsync(IndexName, eventId.ToString());
    }

    public async Task ReindexAllAsync(IEnumerable<Event> events)
    {
        await _client.DeleteByQueryAsync<EventSearchDocument>(IndexName, d => d
            .Query(q => q.MatchAll(new MatchAllQuery())));

        var documents = events.Select(e => new EventSearchDocument
        {
            Id          = e.Id,
            Title       = e.Title,
            Description = e.Description,
            Date        = e.Date,
            Location    = e.Location,
            Price       = e.Price,
            Category    = e.Category,
            ArtistName  = e.ArtistName
        });

        await _client.BulkAsync(b => b
            .Index(IndexName)
            .IndexMany(documents));
    }

    public async Task<IEnumerable<EventDto>> SearchAsync(string query, int page = 1, int pageSize = 20)
    {
        var response = await _client.SearchAsync<EventSearchDocument>(s => s
            .Indices(IndexName)
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .MultiMatch(m => m
                    .Query(query)
                    .Fields(new[]
                    {
                        "title^2",       // titre booste x2
                        "description",
                        "category",
                        "artistName"
                    })
                )
            )
        );

        return response.Documents.Select(d => new EventDto(
            d.Id, d.Title, d.Description, d.Date, d.Location,
            0, d.Price, d.Category, d.ArtistName,
            DateTime.MinValue, null));
    }
}
