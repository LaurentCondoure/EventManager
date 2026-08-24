using EventManager.Domain.Identity.DTOs;

namespace EventManager.Domain.Identity.Interfaces;

/// <summary>
/// Business-facing contract over ASP.NET Core Identity. No business code depends directly on
/// <c>UserManager</c> or <c>SignInManager</c> — this is the only identity surface exposed to them.
/// </summary>
public interface IIdentityService
{
    /// <summary>Finds a user by email address.</summary>
    /// <param name="email">The email address to look up.</param>
    /// <returns>The user's domain-safe projection, or <c>null</c> if no account matches.</returns>
    Task<IdentityUserInfo?> FindByEmailAsync(string email);

    /// <summary>
    /// Verifies a user's password, applying the configured lockout policy on repeated failures.
    /// </summary>
    /// <param name="userId">The user's identifier.</param>
    /// <param name="password">The plaintext password to verify.</param>
    Task<PasswordVerificationResult> VerifyPasswordAsync(Guid userId, string password);

    /// <summary>Returns the role names assigned to a user.</summary>
    /// <param name="userId">The user's identifier.</param>
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId);
}
