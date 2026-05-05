using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;

namespace EventManager.Infrastructure.Search;

/// <summary>
/// 
/// </summary>
public class EventSearchService(ElasticsearchClient client) : IEventSearchService
{
    private const string IndexName = "events";

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

        await client.IndexAsync(document, i => i.Index(IndexName).Id(@event.Id.ToString()));
    }

    public async Task DeleteAsync(Guid eventId)
    {
        await client.DeleteAsync(IndexName, eventId.ToString());
    }

    public async Task ReindexAllAsync(IEnumerable<Event> events)
    {
        await client.DeleteByQueryAsync<EventSearchDocument>(IndexName, d => d
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

        await client.BulkAsync(b => b
            .Index(IndexName)
            .IndexMany(documents));
    }

    public async Task<IEnumerable<SearchResultDto>> SearchAsync(string query, int page = 1, int pageSize = 20)
    {
        var response = await client.SearchAsync<EventSearchDocument>(s => s
            .Indices(IndexName)
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .MultiMatch(m => m
                    .Query(query)
                    .Fields(new[]
                    {
                        "title^2",       // boosted title x2
                        "description",
                        "category",
                        "artistName"
                    })
                )
            )
        );

        if (!response.IsValidResponse)
            throw new InvalidOperationException(
                $"Elasticsearch search unavailable: {response.ElasticsearchServerError?.Error?.Reason}");

        return response.Documents.Select(d => new SearchResultDto(
            d.Id, 
            d.Title, 
            d.Description, 
            d.Date, 
            d.Location,
            d.Price, 
            d.Category, 
            d.ArtistName));
    }
}
