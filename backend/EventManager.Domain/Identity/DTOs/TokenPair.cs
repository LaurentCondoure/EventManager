namespace EventManager.Domain.Identity.DTOs;

/// <summary>
/// An issued access/refresh token pair. <c>RefreshToken</c> is the raw, opaque value — the caller
/// is responsible for cookie delivery; only a hash of it is ever persisted.
/// </summary>
public record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
);
