namespace EventManager.Domain.Identity.Interfaces;

/// <summary>Persists refresh tokens issued at login, for later revocation/rotation.</summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Persists a newly issued refresh token. Implementations must store only a hash of
    /// <paramref name="token"/> — the raw value is never written to the database (ADR-014).
    /// </summary>
    /// <param name="userId">The owning user's identifier.</param>
    /// <param name="token">The raw refresh token, as returned to the caller.</param>
    /// <param name="issuedAt">Issuance timestamp (UTC).</param>
    /// <param name="expiresAt">Expiry timestamp (UTC).</param>
    Task CreateAsync(Guid userId, string token, DateTime issuedAt, DateTime expiresAt);
}
