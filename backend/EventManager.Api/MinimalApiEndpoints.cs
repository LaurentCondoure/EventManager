using EventManager.Api.Auth;
using EventManager.Domain.Events.Constants;
using EventManager.Domain.Events.Interfaces;
using EventManager.Domain.Identity.Constants;
using EventManager.Infrastructure.Options;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventManager.Api;

public static class MinimalApiEndpoints
{
    public static WebApplication MapMinimalApiEndpoints(this WebApplication app)
    {
        // TECH-001 placeholder — proves the cookie-issuance mechanic (Set-Cookie sent, browser
        // round-trips it, JWT Bearer reads and validates it) ahead of TASK-001, which replaces
        // this body with real credential verification via IIdentityService + ITokenService.
        // Always succeeds, ignores any request body, issues no refresh token (needs TECH-002's
        // RefreshTokens table) — this is not a working login, only a wiring proof.
        app.MapPost("/auth/login", (HttpContext context, IOptions<JwtOptions> jwtOptions) =>
        {
            var config  = jwtOptions.Value;
            var expires = DateTime.UtcNow.AddMinutes(config.AccessTokenTtlMinutes);

            Claim[] claims =
            [
                new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000000"),
                new Claim(ClaimTypes.Role, Role.Organizer.ToRoleName()),
                new Claim("must_reset_password", "false")
            ];

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Secret)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(config.Issuer, config.Audience, claims, expires: expires, signingCredentials: credentials);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            context.Response.Cookies.Append(AuthCookieNames.AccessToken, accessToken, AuthCookieOptions.Create(expires));

            return Results.Ok(new { role = Role.Organizer.ToRoleName(), mustResetPassword = false });
        })
           .WithTags("Auth")
           .WithSummary("TECH-001 placeholder — proves cookie issuance; superseded by TASK-001")
           .RequireRateLimiting("fixed");

        // ADR-021: accepts either a staff session (JWT cookie) or the static system key —
        // an admin/super admin can check it manually, and a future automated caller can too,
        // without either needing the other's mechanism.
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
           .WithTags("Health")
           .WithSummary("Health check")
           .RequireRateLimiting("fixed")
           .RequireAuthorization(policy => policy
               .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationDefaults.AuthenticationScheme)
               .RequireAuthenticatedUser());

        app.MapGet("/api/events/categories", () => Results.Ok(EventCategories.All))
           .WithTags("Events")
           .WithSummary("Returns the list of valid event categories")
           .RequireRateLimiting("fixed");

        //According to ADR 011, manually trigger a full reindex of all events from SQL Server to Elasticsearch if needed
        app.MapPost("/admin/search/reindex", async (IEventService eventService) => { await eventService.ReindexAsync(); Results.Ok(); } )
           .WithTags("Search")
           .WithSummary("Reindex elasticsearch.")
           .RequireRateLimiting("fixed");

        return app;
    }
}
