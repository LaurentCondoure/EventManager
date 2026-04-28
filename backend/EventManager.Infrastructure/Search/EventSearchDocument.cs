namespace EventManager.Infrastructure.Search;

/// <summary>Document stored in the Elasticsearch "events" index.</summary>
public class EventSearchDocument
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Date { get; set; }
    public string Location { get; set; } = default!;
    public decimal Price { get; set; }
    public string Category { get; set; } = default!;
    public string? ArtistName { get; set; }
}
