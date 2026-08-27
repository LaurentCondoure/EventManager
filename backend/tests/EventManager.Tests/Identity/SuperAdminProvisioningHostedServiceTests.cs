using EventManager.Domain.Identity.Constants;
using EventManager.Infrastructure.Identity;
using EventManager.Infrastructure.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace EventManager.UnitTests.Identity;

public class SuperAdminProvisioningHostedServiceTests
{
    private static readonly string RoleName = Role.SuperAdmin.ToRoleName();

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    public SuperAdminProvisioningHostedServiceTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task StartAsync_CreatesSuperAdmin_WhenNoneExists()
    {
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync(RoleName)).ReturnsAsync([]);
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Sup3rSecret!1"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleName))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService("admin@example.com", "Sup3rSecret!1");

        await service.StartAsync(TestContext.Current.CancellationToken);

        _userManagerMock.Verify(u => u.CreateAsync(
            It.Is<ApplicationUser>(a =>
                a.Email == "admin@example.com" &&
                a.UserName == "admin@example.com" &&
                a.MustResetPassword &&
                a.IsActive),
            "Sup3rSecret!1"), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleName), Times.Once);
    }

    [Fact]
    public async Task StartAsync_SkipsCreation_WhenSuperAdminAlreadyExists()
    {
        _userManagerMock
            .Setup(u => u.GetUsersInRoleAsync(RoleName))
            .ReturnsAsync([new ApplicationUser { Email = "existing@example.com" }]);

        var service = CreateService("admin@example.com", "Sup3rSecret!1");

        await service.StartAsync(TestContext.Current.CancellationToken);

        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_Throws_WhenUserCreationFails()
    {
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync(RoleName)).ReturnsAsync([]);
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid password" }));

        var service = CreateService("admin@example.com", "bad");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(TestContext.Current.CancellationToken));

        _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    private SuperAdminProvisioningHostedService CreateService(string email, string password)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_userManagerMock.Object);
        ServiceProvider provider = services.BuildServiceProvider();

        return new SuperAdminProvisioningHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new SeedAdminOptions { Email = email, Password = password }),
            NullLogger<SuperAdminProvisioningHostedService>.Instance);
    }
}
