using EventManager.Api.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventManager.IntegrationTests;

/// <summary>
/// Exercises the real JWT Bearer configuration registered in <c>Program.cs</c> (TECH-001):
/// the httpOnly cookie is the sole token carrier, and validation is a standalone, in-memory
/// signature/expiry check — no call into IIdentityService/UserManager per request (ADR-014, ADR-020).
/// </summary>
public class JwtBearerConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestSecret = "integration-test-signing-key-please-ignore-32bytes+";
    private const string Issuer     = "EventManager";
    private const string Audience   = "EventManager";

    private readonly JwtBearerOptions _jwtBearerOptions;

    public JwtBearerConfigurationTests(WebApplicationFactory<Program> factory)
    {
        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            // Deterministic signing key — independent of local user-secrets or CI environment.
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = TestSecret,
                ["Jwt:Issuer"]   = Issuer,
                ["Jwt:Audience"] = Audience
            }));
        });

        var optionsMonitor = configuredFactory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        _jwtBearerOptions = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
    }

    private static string BuildToken(DateTime expires, string secret = TestSecret, string issuer = Issuer, string audience = Audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: expires, signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Cookie extraction (OnMessageReceived) ───────────────────────────────

    [Fact]
    public async Task OnMessageReceived_ShouldExtractToken_FromAccessTokenCookie()
    {
        var token = BuildToken(DateTime.UtcNow.AddMinutes(5));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"{AuthCookieNames.AccessToken}={token}");

        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        var messageReceivedContext = new MessageReceivedContext(httpContext, scheme, _jwtBearerOptions);

        await _jwtBearerOptions.Events!.OnMessageReceived(messageReceivedContext);

        messageReceivedContext.Token.Should().Be(token);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldLeaveTokenUnset_WhenCookieIsAbsent()
    {
        var httpContext = new DefaultHttpContext();

        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        var messageReceivedContext = new MessageReceivedContext(httpContext, scheme, _jwtBearerOptions);

        await _jwtBearerOptions.Events!.OnMessageReceived(messageReceivedContext);

        messageReceivedContext.Token.Should().BeNull();
    }

    // ── Signature / expiry validation (TokenValidationParameters) ───────────

    [Fact]
    public void ValidateToken_ShouldSucceed_ForValidToken()
    {
        var token = BuildToken(DateTime.UtcNow.AddMinutes(5));

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token, _jwtBearerOptions.TokenValidationParameters, out _);

        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateToken_ShouldThrow_ForExpiredToken()
    {
        var token = BuildToken(DateTime.UtcNow.AddMinutes(-5));

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(
            token, _jwtBearerOptions.TokenValidationParameters, out _);

        act.Should().Throw<SecurityTokenExpiredException>();
    }

    [Fact]
    public void ValidateToken_ShouldThrow_ForTokenSignedWithWrongKey()
    {
        var token = BuildToken(DateTime.UtcNow.AddMinutes(5), secret: "a-completely-different-signing-key-32bytes+");

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(
            token, _jwtBearerOptions.TokenValidationParameters, out _);

        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    [Fact]
    public void ValidateToken_ShouldThrow_ForWrongIssuer()
    {
        var token = BuildToken(DateTime.UtcNow.AddMinutes(5), issuer: "SomeoneElse");

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(
            token, _jwtBearerOptions.TokenValidationParameters, out _);

        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }
}
