using EventManager.Domain.Identity.Constants;
using EventManager.Infrastructure.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventManager.Infrastructure.Identity;

/// <summary>
/// Provisions the first super admin account at startup (ADR-017), so login can be tested
/// end-to-end on a fresh database without a manual step.
/// </summary>
/// <remarks>
/// Must run after Identity migrations are applied — registered after
/// <see cref="Migrations.DatabaseMigrationHostedService"/> so hosted services start in order.
/// The <c>super_admin</c> role row itself is static reference data seeded by migration
/// (<see cref="EventManagerIdentityDbContext"/>), not by this service — only the account depends
/// on per-environment secrets.
/// </remarks>
public sealed class SuperAdminProvisioningHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SeedAdminOptions> seedAdminOptions,
    ILogger<SuperAdminProvisioningHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string roleName = Role.SuperAdmin.ToRoleName();
        SeedAdminOptions options = seedAdminOptions.Value;

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        IList<ApplicationUser> existingSuperAdmins = await userManager.GetUsersInRoleAsync(roleName);
        if (existingSuperAdmins.Count > 0)
        {
            logger.LogInformation("Super admin provisioning: skipped — an account with role {Role} already exists", roleName);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
            IsActive = true,
            MustResetPassword = true
        };

        IdentityResult createResult = await userManager.CreateAsync(user, options.Password);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Super admin provisioning failed: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            throw new InvalidOperationException("Super admin provisioning failed — see logs for details.");
        }

        await userManager.AddToRoleAsync(user, roleName);

        logger.LogInformation("Super admin provisioning: created account for role {Role}", roleName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
