using EventManager.Domain.Identity.DTOs;

namespace EventManager.Domain.Identity.Interfaces;

/// <summary>Business logic contract for authentication.</summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user by email and password, issuing a fresh access/refresh token pair
    /// and persisting the refresh token on success.
    /// </summary>
    /// <param name="email">The account's email address.</param>
    /// <param name="password">The plaintext password to verify.</param>
    /// <exception cref="EventManager.Domain.Exceptions.UnauthorizedException">
    /// Thrown when the account does not exist, the password is invalid, or the account is
    /// deactivated. Account-not-found and wrong-password carry no <c>ErrorCode</c> so the
    /// client response is identical for both — no information leakage (ADR-014).
    /// </exception>
    Task<LoginResult> LoginAsync(string email, string password);
}
