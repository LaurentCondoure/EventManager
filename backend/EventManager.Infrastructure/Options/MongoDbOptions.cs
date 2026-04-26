namespace EventManager.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for MongoDB connection settings.
/// </summary>
public sealed class MongoDbOptions
{
    /// <summary>
    /// The configuration section name this class binds to (<c>MongoDb</c>).
    /// </summary>
    public const string SectionName = "MongoDb";
    /// <summary>
    /// Connection string for MongoDB, including credentials and server address.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Name of the MongoDB database to connect to.
    /// </summary>
    public string DatabaseName { get; init; } = string.Empty;
}