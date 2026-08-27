using EventManager.Domain.Identity.Constants;
using EventManager.Infrastructure.Identity;
using EventManager.Infrastructure.Options;
using EventManager.InfrastructureTests.Fixtures;

using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EventManager.InfrastructureTests.Identity;

public sealed class SuperAdminProvisioningHostedServiceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public SuperAdminProvisioningHostedServiceTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_SeedsTheThreeStaticRoles()
    {
        string connectionString = await _fixture.CreateMigratedIdentityDatabaseAsync();
        await using ServiceProvider provider = BuildProvider(connectionString);

        using IServiceScope scope = provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (Role role in Enum.GetValues<Role>())
        {
            string roleName = role.ToRoleName();
            IdentityRole<Guid>? seededRole = await roleManager.FindByNameAsync(roleName);

            seededRole.Should().NotBeNull($"role '{roleName}' should be seeded by the Identity migration");
            seededRole!.Id.Should().Be(role.ToRoleId());
        }
    }

    [Fact]
    public async Task StartAsync_CreatesSuperAdmin_OnFreshDatabase()
    {
        string connectionString = await _fixture.CreateMigratedIdentityDatabaseAsync();
        await using ServiceProvider provider = BuildProvider(connectionString);

        var service = CreateService(provider, "seed-admin@example.com", "Sup3rSecret!1");
        await service.StartAsync(TestContext.Current.CancellationToken);

        using IServiceScope scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser? user = await userManager.FindByEmailAsync("seed-admin@example.com");

        user.Should().NotBeNull();
        user!.MustResetPassword.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        (await userManager.GetRolesAsync(user)).Should().ContainSingle(Role.SuperAdmin.ToRoleName());
    }

    [Fact]
    public async Task StartAsync_CalledTwice_DoesNotThrowOrDuplicate()
    {
        string connectionString = await _fixture.CreateMigratedIdentityDatabaseAsync();
        await using ServiceProvider provider = BuildProvider(connectionString);

        var service = CreateService(provider, "seed-admin@example.com", "Sup3rSecret!1");
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);

        using IServiceScope scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        IList<ApplicationUser> superAdmins = await userManager.GetUsersInRoleAsync(Role.SuperAdmin.ToRoleName());
        superAdmins.Should().ContainSingle();
    }

    [Fact]
    public async Task StartAsync_SkipsCreation_WhenSuperAdminAlreadyExists()
    {
        string connectionString = await _fixture.CreateMigratedIdentityDatabaseAsync();
        await using ServiceProvider provider = BuildProvider(connectionString);

        var firstService = CreateService(provider, "first-admin@example.com", "Sup3rSecret!1");
        await firstService.StartAsync(TestContext.Current.CancellationToken);

        var secondService = CreateService(provider, "second-admin@example.com", "Sup3rSecret!1");
        await secondService.StartAsync(TestContext.Current.CancellationToken);

        using IServiceScope scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        (await userManager.FindByEmailAsync("second-admin@example.com")).Should().BeNull();
        (await userManager.GetUsersInRoleAsync(Role.SuperAdmin.ToRoleName())).Should().ContainSingle();
    }

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<EventManagerIdentityDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EventManagerIdentityDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private static SuperAdminProvisioningHostedService CreateService(ServiceProvider provider, string email, string password) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new SeedAdminOptions { Email = email, Password = password }),
            NullLogger<SuperAdminProvisioningHostedService>.Instance);
}
