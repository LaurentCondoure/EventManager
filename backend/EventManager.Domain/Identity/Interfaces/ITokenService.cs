using EventManager.Domain.Identity.Constants;
using EventManager.Domain.Identity.DTOs;

namespace EventManager.Domain.Identity.Interfaces;

/// <summary>Issues signed access tokens and opaque refresh tokens (ADR-014).</summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a new access/refresh token pair for an authenticated user.
    /// </summary>
    /// <param name="userId">The user's identifier — embedded as the access token's subject claim.</param>
    /// <param name="role">The user's role — embedded as the access token's role claim.</param>
    /// <param name="mustResetPassword">Embedded as the <c>must_reset_password</c> claim (ADR-014).</param>
    TokenPair GenerateTokenPair(Guid userId, Role role, bool mustResetPassword);
}
