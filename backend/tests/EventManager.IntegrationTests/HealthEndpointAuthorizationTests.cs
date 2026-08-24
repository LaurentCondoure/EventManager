using EventManager.Api.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EventManager.IntegrationTests;

/// <summary>
/// End-to-end check that <c>GET /health</c> is a protected route through the real pipeline
/// (ADR-014 JWT Bearer + httpOnly cookie): unauthenticated requests are rejected, and a request
/// carrying a valid access token cookie is let through.
/// </summary>
public class HealthEndpointAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestSecret = "integration-test-signing-key-please-ignore-32bytes+";
    private const string Issuer     = "EventManager";
    private const string Audience   = "EventManager";
    private const string TestApiKey = "integration-test-system-api-key";

    private readonly HttpClient _client;

    public HealthEndpointAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = TestSecret,
                ["Jwt:Issuer"]   = Issuer,
                ["Jwt:Audience"] = Audience,
                ["ApiKey:Value"] = TestApiKey
            }));
        }).CreateClient();
    }

    private static string BuildToken(DateTime expires) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            Issuer, Audience, [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)), SecurityAlgorithms.HmacSha256)));

    [Fact]
    public async Task Health_ShouldReturnUnauthorized_WhenNoAccessTokenCookieIsPresent()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_ShouldReturnOk_WhenAccessTokenCookieIsValid()
    {
        var token = BuildToken(DateTime.UtcNow.AddMinutes(5));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Cookie", $"{AuthCookieNames.AccessToken}={token}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ShouldReturnUnauthorized_WhenAccessTokenCookieIsExpired()
    {
        var token = BuildToken(DateTime.UtcNow.AddMinutes(-5));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Cookie", $"{AuthCookieNames.AccessToken}={token}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── System API key (ADR-021) ─────────────────────────────────────────────

    [Fact]
    public async Task Health_ShouldReturnOk_WhenApiKeyHeaderIsValid()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(ApiKeyAuthenticationDefaults.HeaderName, TestApiKey);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ShouldReturnUnauthorized_WhenApiKeyHeaderIsWrong()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(ApiKeyAuthenticationDefaults.HeaderName, "not-the-configured-key");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
