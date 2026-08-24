namespace EventManager.Domain.Identity.Constants;

/// <summary>
/// Maps <see cref="Role"/> to the exact string value ASP.NET Core Identity and JWT role claims
/// expect. The single place the string spelling (including <c>super_admin</c>'s snake_case) is
/// defined — everything else works with <see cref="Role"/>.
/// </summary>
public static class Roles
{
    public static string ToRoleName(this Role role) => role switch
    {
        Role.Organizer  => "organizer",
        Role.Admin      => "admin",
        Role.SuperAdmin => "super_admin",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}
