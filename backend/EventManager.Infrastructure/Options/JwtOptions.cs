namespace EventManager.Infrastructure.Options;

/// <summary>Strongly-typed configuration for JWT issuance and validation (ADR-014).</summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name this class binds to (<c>Jwt</c>).</summary>
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key. Must be supplied via environment variable — never hardcoded.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Expected token issuer.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Expected token audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Access token time-to-live, in minutes.</summary>
    public int AccessTokenTtlMinutes { get; set; } = 10;

    /// <summary>Refresh token time-to-live, in hours.</summary>
    public int RefreshTokenTtlHours { get; set; } = 8;
}
