using EventManager.Domain.Identity.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.Identity;

/// <summary>
/// EF Core context for the <c>EventManager_Identity</c> database (ADR-015). Holds the default
/// ASP.NET Core Identity schema for <see cref="ApplicationUser"/>.
/// </summary>
/// <remarks>
/// <c>RefreshTokens</c> and <c>PasswordHistory</c> — and the initial migration covering this
/// context — are added by TECH-002.
/// </remarks>
public class EventManagerIdentityDbContext(DbContextOptions<EventManagerIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordHistory> PasswordHistory => Set<PasswordHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Static reference data (ADR-016): the three V1 roles, seeded via migration rather than
        // at runtime, unlike the super admin account which depends on per-environment secrets
        // (ADR-017) and is provisioned by SuperAdminProvisioningHostedService instead.
        builder.Entity<IdentityRole<Guid>>().HasData(
            BuildRole(Role.Organizer),
            BuildRole(Role.Admin),
            BuildRole(Role.SuperAdmin));
    }

    private static IdentityRole<Guid> BuildRole(Role role)
    {
        string name = role.ToRoleName();
        return new IdentityRole<Guid>
        {
            Id = role.ToRoleId(),
            Name = name,
            NormalizedName = name.ToUpperInvariant()
        };
    }
}
