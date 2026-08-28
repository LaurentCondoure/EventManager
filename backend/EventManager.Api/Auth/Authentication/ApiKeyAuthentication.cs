using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using EventManager.Infrastructure.Options;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EventManager.Api.Auth.Authentication;

/// <summary>Scheme name and header name for the static system API key (ADR-021).</summary>
public static class ApiKeyAuthenticationDefaults
{
    /// <summary>Name this scheme is registered under in <c>AddAuthentication</c>.</summary>
    public const string AuthenticationScheme = "ApiKey";

    /// <summary>Request header carrying the key.</summary>
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// Authenticates a caller presenting the configured static key in the <see cref="ApiKeyAuthenticationDefaults.HeaderName"/>
/// header. Intended for automated/system callers that shouldn't need the human login/cookie flow (ADR-014, ADR-021).
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ApiKeyOptions> apiKeyOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var providedKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        var configuredKey = apiKeyOptions.Value.Value;

        // Constant-time comparison — a static shared secret must not be checkable via a timing side-channel.
        if (string.IsNullOrEmpty(configuredKey) || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedKey.ToString()),
                Encoding.UTF8.GetBytes(configuredKey)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "system"), new Claim(ClaimTypes.AuthenticationMethod, ApiKeyAuthenticationDefaults.AuthenticationScheme)],
            ApiKeyAuthenticationDefaults.AuthenticationScheme);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), ApiKeyAuthenticationDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
