namespace EventManager.Domain.Identity.Constants;

/// <summary>
/// The three V1 roles (ADR-016), ordered by rank — declaration order is the rank order,
/// so <c>SuperAdmin > Admin > Organizer</c> holds via plain ordinal comparison.
/// </summary>
public enum Role
{
    Organizer,
    Admin,
    SuperAdmin
}
