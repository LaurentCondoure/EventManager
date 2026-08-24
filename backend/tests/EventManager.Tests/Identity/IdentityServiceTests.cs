using EventManager.Domain.Identity.Constants;
using EventManager.Domain.Identity.DTOs;
using EventManager.Infrastructure.Identity;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

using PasswordVerificationResult = EventManager.Domain.Identity.DTOs.PasswordVerificationResult;

namespace EventManager.UnitTests.Identity;

public class IdentityServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>>   _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly IdentityService _sut;

    public IdentityServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory   = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

        _sut = new IdentityService(_userManagerMock.Object, _signInManagerMock.Object);
    }

    private static ApplicationUser BuildUser(Guid id, string email, bool isActive = true, bool mustResetPassword = false) =>
        new()
        {
            Id                = id,
            Email             = email,
            IsActive          = isActive,
            MustResetPassword = mustResetPassword
        };

    // ── FindByEmailAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnMappedInfo_WhenUserExists()
    {
        var user = BuildUser(Guid.NewGuid(), "marie@example.com", isActive: true, mustResetPassword: true);
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        IdentityUserInfo? result = await _sut.FindByEmailAsync(user.Email!);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.IsActive.Should().BeTrue();
        result.MustResetPassword.Should().BeTrue();
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("unknown@example.com")).ReturnsAsync((ApplicationUser?)null);

        IdentityUserInfo? result = await _sut.FindByEmailAsync("unknown@example.com");

        result.Should().BeNull();
    }

    // ── VerifyPasswordAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task VerifyPasswordAsync_ShouldReturnSuccess_WhenPasswordIsCorrect()
    {
        var user = BuildUser(Guid.NewGuid(), "marie@example.com");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(user, "correct-password", true))
            .ReturnsAsync(SignInResult.Success);

        PasswordVerificationResult result = await _sut.VerifyPasswordAsync(user.Id, "correct-password");

        result.Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ShouldReturnFailed_WhenPasswordIsWrong()
    {
        var user = BuildUser(Guid.NewGuid(), "marie@example.com");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(user, "wrong-password", true))
            .ReturnsAsync(SignInResult.Failed);

        PasswordVerificationResult result = await _sut.VerifyPasswordAsync(user.Id, "wrong-password");

        result.Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ShouldReturnLockedOut_WhenAccountIsLockedOut()
    {
        var user = BuildUser(Guid.NewGuid(), "marie@example.com");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(user, "irrelevant", true))
            .ReturnsAsync(SignInResult.LockedOut);

        PasswordVerificationResult result = await _sut.VerifyPasswordAsync(user.Id, "irrelevant");

        result.Should().Be(PasswordVerificationResult.LockedOut);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ShouldReturnFailed_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        PasswordVerificationResult result = await _sut.VerifyPasswordAsync(userId, "any-password");

        result.Should().Be(PasswordVerificationResult.Failed);
        _signInManagerMock.Verify(
            s => s.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    // ── GetRolesAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRolesAsync_ShouldReturnRoles_WhenUserExists()
    {
        var user = BuildUser(Guid.NewGuid(), "marie@example.com");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([Role.Organizer.ToRoleName()]);

        IReadOnlyList<string> roles = await _sut.GetRolesAsync(user.Id);

        roles.Should().ContainSingle().Which.Should().Be(Role.Organizer.ToRoleName());
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnEmpty_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        IReadOnlyList<string> roles = await _sut.GetRolesAsync(userId);

        roles.Should().BeEmpty();
    }
}
