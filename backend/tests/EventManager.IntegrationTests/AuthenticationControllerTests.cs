using EventManager.Api.Auth.Authentication;
using EventManager.Domain.Identity.Constants;
using EventManager.Domain.Identity.DTOs;
using EventManager.Domain.Identity.Interfaces;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

using PasswordVerificationResult = EventManager.Domain.Identity.DTOs.PasswordVerificationResult;

namespace EventManager.IntegrationTests;

/// <summary>
/// Integration tests for <c>POST /auth/login</c> (TASK-001). Exercises the real ASP.NET Core
/// pipeline — routing, FluentValidation, the real <see cref="EventManager.Domain.Identity.Services.AuthenticationService"/>
/// and the real <c>TokenService</c> — with only <see cref="IIdentityService"/> and
/// <see cref="IRefreshTokenRepository"/> replaced by mocks, so no SQL Server is required.
/// N.B: creates its client with an https:// base address so the automatic cookie jar honours the
/// Secure cookie attribute (ADR-014) the same way a real browser would.
/// </summary>
public class AuthenticationControllerTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private const string TestSecret = "integration-test-signing-key-please-ignore-32bytes+";

    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly HttpClient _client;

    public AuthenticationControllerTests(IntegrationTestWebApplicationFactory factory)
    {
        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = TestSecret,
                ["Jwt:Issuer"]   = "EventManager",
                ["Jwt:Audience"] = "EventManager"
            }));

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IIdentityService>();
                services.AddScoped(_ => _identityServiceMock.Object);

                services.RemoveAll<IRefreshTokenRepository>();
                services.AddScoped(_ => _refreshTokenRepositoryMock.Object);
            });
        });

        _client = configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static IdentityUserInfo BuildUser(string email = "marie@example.com", bool isActive = true, bool mustResetPassword = false) =>
        new(Guid.NewGuid(), email, isActive, mustResetPassword);

    [Fact]
    public async Task Login_ShouldSetBothCookiesAndReturnBody_WhenCredentialsAreValid()
    {
        var user = BuildUser(mustResetPassword: true);
        _identityServiceMock.Setup(s => s.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _identityServiceMock.Setup(s => s.VerifyPasswordAsync(user.Id, "correct-password"))
            .ReturnsAsync(PasswordVerificationResult.Success);
        _identityServiceMock.Setup(s => s.GetRolesAsync(user.Id))
            .ReturnsAsync([Role.Organizer.ToRoleName()]);

        var response = await _client.PostAsJsonAsync(
            "/auth/login", new LoginInput(user.Email, "correct-password"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(TestContext.Current.CancellationToken);
        body!.Role.Should().Be(Role.Organizer.ToRoleName());
        body.MustResetPassword.Should().BeTrue();

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith($"{AuthCookieNames.AccessToken}="));
        cookies!.Should().Contain(c => c.StartsWith($"{AuthCookieNames.RefreshToken}="));
        cookies!.Should().OnlyContain(c => c.Contains("httponly") && c.Contains("samesite=strict"), "ADR-014 requires HttpOnly + SameSite=Strict on both cookies");

        var accessTokenCookie = cookies!.Single(c => c.StartsWith($"{AuthCookieNames.AccessToken}="));
        var token = accessTokenCookie.Split(';')[0].Split('=', 2)[1];
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "must_reset_password" && c.Value == "true");
        jwt.Claims.Should().Contain(c => c.Type.EndsWith("role") && c.Value == Role.Organizer.ToRoleName());

        _refreshTokenRepositoryMock.Verify(
            r => r.CreateAsync(user.Id, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenAccountDoesNotExist()
    {
        _identityServiceMock.Setup(s => s.FindByEmailAsync("unknown@example.com")).ReturnsAsync((IdentityUserInfo?)null);

        var response = await _client.PostAsJsonAsync(
            "/auth/login", new LoginInput("unknown@example.com", "whatever"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Fact]
    public async Task Login_ShouldReturnIdenticalBody_ForWrongPasswordAndUnknownAccount()
    {
        var user = BuildUser();
        _identityServiceMock.Setup(s => s.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _identityServiceMock.Setup(s => s.VerifyPasswordAsync(user.Id, "wrong-password"))
            .ReturnsAsync(PasswordVerificationResult.Failed);
        _identityServiceMock.Setup(s => s.FindByEmailAsync("unknown@example.com")).ReturnsAsync((IdentityUserInfo?)null);

        var wrongPasswordResponse = await _client.PostAsJsonAsync(
            "/auth/login", new LoginInput(user.Email, "wrong-password"), TestContext.Current.CancellationToken);
        var unknownAccountResponse = await _client.PostAsJsonAsync(
            "/auth/login", new LoginInput("unknown@example.com", "whatever"), TestContext.Current.CancellationToken);

        wrongPasswordResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownAccountResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadFromJsonAsync<ProblemBody>(TestContext.Current.CancellationToken);
        var unknownAccountBody = await unknownAccountResponse.Content.ReadFromJsonAsync<ProblemBody>(TestContext.Current.CancellationToken);
        wrongPasswordBody.Should().NotBeNull();
        unknownAccountBody.Should().NotBeNull();

        // RequestId is excluded from the comparison — it's the per-request TraceIdentifier, not
        // information about which of the two cases occurred, so it legitimately differs every call.
        wrongPasswordBody.Should().BeEquivalentTo(unknownAccountBody, options => options.Excluding(b => b.RequestId),
            "no information leakage between the two cases (ADR-014)");
        wrongPasswordBody.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task Login_ShouldReturnAccountDeactivatedErrorCode_WhenAccountIsInactive()
    {
        var user = BuildUser(isActive: false);
        _identityServiceMock.Setup(s => s.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var response = await _client.PostAsJsonAsync(
            "/auth/login", new LoginInput(user.Email, "whatever"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("ACCOUNT_DEACTIVATED");

        _identityServiceMock.Verify(s => s.VerifyPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenPasswordIsMissing()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login", new LoginInput("marie@example.com", ""), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenEmailIsMissing()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login", new LoginInput("", "some-password"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>The subset of the ProblemDetails shape these tests assert on.</summary>
    private record ProblemBody(string Title, string Detail, int Status, string RequestId, string? ErrorCode);
}
