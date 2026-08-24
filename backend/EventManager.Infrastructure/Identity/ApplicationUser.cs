using Microsoft.AspNetCore.Identity;

namespace EventManager.Infrastructure.Identity;

/// <summary>ASP.NET Core Identity user, extended with the account state fields V1 requires.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Whether the account can authenticate. Set to <c>false</c> on deactivation.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether the user must reset their password before accessing protected features.</summary>
    public bool MustResetPassword { get; set; }
}
