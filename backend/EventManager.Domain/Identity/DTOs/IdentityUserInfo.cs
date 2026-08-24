namespace EventManager.Domain.Identity.DTOs;

/// <summary>Domain-safe projection of an identity user, exposed by <see cref="IIdentityService"/>.</summary>
public record IdentityUserInfo(
    /// <summary>Unique identifier of the user.</summary>
    Guid Id,
    /// <summary>Email address of the user.</summary>
    string Email,
    /// <summary>Whether the account is active. A deactivated account cannot authenticate.</summary>
    bool IsActive,
    /// <summary>Whether the user must reset their password before accessing protected features.</summary>
    bool MustResetPassword
);
