using EventManager.Domain.Identity.Constants;

namespace EventManager.Domain.Identity.DTOs;

/// <summary>Outcome of a successful <see cref="Interfaces.IAuthenticationService.LoginAsync"/> call.</summary>
public record LoginResult(
    /// <summary>The issued access/refresh token pair.</summary>
    TokenPair Tokens,
    /// <summary>The authenticated user's role.</summary>
    Role Role,
    /// <summary>Whether the user must reset their password before accessing protected features.</summary>
    bool MustResetPassword
);
