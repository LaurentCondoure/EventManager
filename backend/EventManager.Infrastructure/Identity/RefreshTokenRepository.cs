using EventManager.Domain.Identity.Interfaces;

using System.Security.Cryptography;
using System.Text;

namespace EventManager.Infrastructure.Identity;

/// <summary>Persists refresh tokens to <c>EventManager_Identity</c> via EF Core (ADR-015, ADR-018).</summary>
public sealed class RefreshTokenRepository(EventManagerIdentityDbContext dbContext) : IRefreshTokenRepository
{
    /// <inheritdoc/>
    public async Task CreateAsync(Guid userId, string token, DateTime issuedAt, DateTime expiresAt)
    {
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Hash(token),
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        });

        await dbContext.SaveChangesAsync();
    }

    /// <summary>Only the hash is ever persisted — the raw refresh token exists solely in the caller's cookie.</summary>
    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
