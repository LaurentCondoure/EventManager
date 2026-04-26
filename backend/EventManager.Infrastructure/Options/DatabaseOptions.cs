namespace EventManager.Infrastructure.Options;

/// <summary>Strongly-typed configuration for database connection settings.</summary>
public sealed class DatabaseOptions
{
    /// <summary>The configuration section name this class binds to (<c>ConnectionStrings</c>).</summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>Connection string for the primary SQL Server database.</summary>
    public string DefaultConnection { get; set; } = string.Empty;
}
