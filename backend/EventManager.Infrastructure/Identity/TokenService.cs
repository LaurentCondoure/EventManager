using EventManager.Domain.Identity.Constants;
using EventManager.Domain.Identity.DTOs;
using EventManager.Domain.Identity.Interfaces;
using EventManager.Infrastructure.Options;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventManager.Infrastructure.Identity;

/// <summary>Issues JWT access tokens and opaque refresh tokens per ADR-014.</summary>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    /// <inheritdoc/>
    public TokenPair GenerateTokenPair(Guid userId, Role role, bool mustResetPassword)
    {
        var config = jwtOptions.Value;
        var now = DateTime.UtcNow;
        var accessTokenExpiresAt = now.AddMinutes(config.AccessTokenTtlMinutes);
        var refreshTokenExpiresAt = now.AddHours(config.RefreshTokenTtlHours);

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToRoleName()),
            new Claim("must_reset_password", mustResetPassword ? "true" : "false")
        ];

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Secret)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(config.Issuer, config.Audience, claims, expires: accessTokenExpiresAt, signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new TokenPair(accessToken, GenerateOpaqueToken(), accessTokenExpiresAt, refreshTokenExpiresAt);
    }

    /// <summary>Cryptographically random refresh token — opaque, not a JWT (revoked/rotated server-side only).</summary>
    private static string GenerateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
