using EventManager.Domain.Identity.Constants;
using EventManager.Domain.Identity.Interfaces;

namespace EventManager.Domain.Identity.Entities;

/// <summary>
/// Domain-owned view of a user, returned by <see cref="IIdentityService"/>.
/// Deliberately independent of ASP.NET Core Identity's <c>IdentityUser</c> — Domain must not
/// depend on the Identity framework, even as a base class. Infrastructure maps its Identity
/// entity to this shape at the boundary.
/// </summary>
public sealed class AuthenticatedUser
{
    /// <summary>Identity store's user id (a GUID string, per <c>IdentityUser</c>'s default key type).</summary>
    public required string Id { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required Role Role { get; init; }

    public required bool IsActive { get; init; }

    public required bool MustResetPassword { get; init; }
}
