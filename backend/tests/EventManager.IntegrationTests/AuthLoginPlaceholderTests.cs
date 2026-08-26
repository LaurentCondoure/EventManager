using EventManager.Api.Auth;
using EventManager.Domain.Identity.Constants;

using System.IdentityModel.Tokens.Jwt;
using System.Net;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EventManager.IntegrationTests;

/// <summary>
/// Integration tests for the TECH-001 cookie-issuance mechanic: <c>POST /auth/login</c> (the
/// placeholder — superseded by TASK-001) actually sets a <c>Set-Cookie</c> header, and that
/// exact cookie is then accepted by JWT Bearer on a real protected route.
/// N.B : creates its client with an https:// base address (WebApplicationFactoryClientOptions.BaseAddress). 
/// The in-memory TestServer represent the connection as HTTPS without a real TLS handshake
/// and relies entirely on the automatic cookie jar, no hand-crafted header.
/// </summary>
public class AuthLoginPlaceholderTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private const string TestSecret = "integration-test-signing-key-please-ignore-32bytes+";

    private readonly HttpClient _client;

    public AuthLoginPlaceholderTests(IntegrationTestWebApplicationFactory factory)
    {
        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = TestSecret,
                ["Jwt:Issuer"]   = "EventManager",
                ["Jwt:Audience"] = "EventManager"
            }));
        });

        // AuthCookieOptions marks the cookie Secure (ADR-014) — a real client only sends it back
        // over HTTPS. An https:// base address makes the in-memory test host represent the
        // connection as such, so the client's automatic cookie jar behaves like a real browser
        // instead of needing to bypass that enforcement by hand-crafting a Cookie header.
        _client = configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Login_ShouldSetAccessTokenCookie_WithExpectedClaims()
    {
        var response = await _client.PostAsync("/auth/login", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

        var accessTokenCookie = cookies!.Single(c => c.StartsWith($"{AuthCookieNames.AccessToken}="));
        accessTokenCookie.Should().Contain("httponly");
        accessTokenCookie.Should().Contain("samesite=strict", "ADR-014 requires SameSite=Strict");

        var token = accessTokenCookie.Split(';')[0].Split('=', 2)[1];
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "must_reset_password" && c.Value == "false");
        jwt.Claims.Should().Contain(c => c.Type.EndsWith("role") && c.Value == Role.Organizer.ToRoleName());
    }

    [Fact]
    public async Task Login_ThenHealth_ShouldSucceed_UsingTheIssuedCookie()
    {
        // No manual Cookie header here
        // It only carries the cookie forward if it actually honours Secure/SameSite the way a real browser would;
        var loginResponse = await _client.PostAsync("/auth/login", content: null, TestContext.Current.CancellationToken);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthResponse = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
