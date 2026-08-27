namespace EventManager.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for the first super admin seed (ADR-017). Bound directly from
/// the flat <c>SEED_ADMIN_EMAIL</c> / <c>SEED_ADMIN_PASSWORD</c> environment variables — no
/// nested section, per the ADR's literal naming.
/// </summary>
public sealed class SeedAdminOptions
{
    /// <summary>Email of the seeded super admin. Must be supplied via <c>SEED_ADMIN_EMAIL</c>.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Password of the seeded super admin. Must be supplied via <c>SEED_ADMIN_PASSWORD</c>.</summary>
    public string Password { get; set; } = string.Empty;
}
