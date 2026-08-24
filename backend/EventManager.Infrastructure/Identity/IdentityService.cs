using EventManager.Domain.Identity.DTOs;
using EventManager.Domain.Identity.Interfaces;

using Microsoft.AspNetCore.Identity;

using PasswordVerificationResult = EventManager.Domain.Identity.DTOs.PasswordVerificationResult;

namespace EventManager.Infrastructure.Identity;

/// <summary>
/// Wraps <see cref="UserManager{TUser}"/> and <see cref="SignInManager{TUser}"/> behind
/// <see cref="IIdentityService"/> so no Identity type leaks past the Infrastructure layer.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IIdentityService
{
    /// <inheritdoc/>
    public async Task<IdentityUserInfo?> FindByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : ToIdentityUserInfo(user);
    }

    /// <inheritdoc/>
    public async Task<PasswordVerificationResult> VerifyPasswordAsync(Guid userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return PasswordVerificationResult.Failed;

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return PasswordVerificationResult.LockedOut;

        return result.Succeeded ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return [];

        var roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    private static IdentityUserInfo ToIdentityUserInfo(ApplicationUser user) =>
        new(user.Id, user.Email!, user.IsActive, user.MustResetPassword);
}
