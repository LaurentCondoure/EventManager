namespace EventManager.Infrastructure.Options;

/// <summary>Strongly-typed configuration for database connection settings.</summary>
public class RedisOptions
{
    /// <summary>The configuration section name this class binds to (<c>ConnectionStrings</c>).</summary>
    public const string SectionName = "Redis";

    /// <summary>Gets or sets the Redis connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the time-to-live for Redis entries.</summary>
    public int TimeToLive { get; set; } = 0;
}
