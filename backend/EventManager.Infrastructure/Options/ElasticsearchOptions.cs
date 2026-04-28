namespace EventManager.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for Elasticsearch connection settings.
/// </summary>
public sealed class ElasticsearchOptions
{
    /// <summary>
    /// The configuration section name this class binds to (<c>Elasticsearch</c>).
    /// </summary>
    public const string SectionName = "Elasticsearch";

    /// <summary>
    /// Base URL of the Elasticsearch node (e.g. <c>http://localhost:9200</c>).
    /// </summary>
    public string Url { get; init; } = string.Empty;
}
