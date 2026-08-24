namespace EventManager.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for the static system API key (ADR-021). Lets an automated
/// caller authenticate without going through the human login/cookie flow (ADR-014).
/// </summary>
public sealed class ApiKeyOptions
{
    /// <summary>The configuration section name this class binds to (<c>ApiKey</c>).</summary>
    public const string SectionName = "ApiKey";

    /// <summary>The expected key value. Must be supplied via environment variable — never hardcoded.</summary>
    public string Value { get; set; } = string.Empty;
}
