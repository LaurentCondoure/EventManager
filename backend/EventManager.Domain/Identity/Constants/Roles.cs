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

    /// <summary>
    /// Fixed identifier for the role's <c>AspNetRoles</c> row, seeded by an EF Core migration
    /// (the three V1 roles are static reference data, not runtime-provisioned). Never change an
    /// existing role's id — it is a stable foreign key from <c>AspNetUserRoles</c>.
    /// </summary>
    public static Guid ToRoleId(this Role role) => role switch
    {
        Role.Organizer  => Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Role.Admin      => Guid.Parse("00000000-0000-0000-0000-000000000002"),
        Role.SuperAdmin => Guid.Parse("00000000-0000-0000-0000-000000000003"),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}
