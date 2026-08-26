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
}
