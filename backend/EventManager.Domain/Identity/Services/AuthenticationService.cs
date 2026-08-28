using EventManager.Domain.Exceptions;
using EventManager.Domain.Identity.Constants;
using EventManager.Domain.Identity.DTOs;
using EventManager.Domain.Identity.Interfaces;

namespace EventManager.Domain.Identity.Services;

/// <summary>Implements authentication business logic (ADR-014).</summary>
public class AuthenticationService(
    IIdentityService identityService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var user = await identityService.FindByEmailAsync(email)
            ?? throw new UnauthorizedException($"Login failed — no account for '{email}'.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException(
                $"Login failed — account '{email}' is deactivated.",
                AuthenticationErrorCode.AccountDeactivated.ToErrorCode());
        }

        var verification = await identityService.VerifyPasswordAsync(user.Id, password);
        if (verification != PasswordVerificationResult.Success)
            throw new UnauthorizedException($"Login failed — invalid password for '{email}'.");

        var roles = await identityService.GetRolesAsync(user.Id);
        var role = roles.Select(r => r.FromRoleName()).Single();

        var tokens = tokenService.GenerateTokenPair(user.Id, role, user.MustResetPassword);
        await refreshTokenRepository.CreateAsync(user.Id, tokens.RefreshToken, DateTime.UtcNow, tokens.RefreshTokenExpiresAt);

        return new LoginResult(tokens, role, user.MustResetPassword);
    }
}
