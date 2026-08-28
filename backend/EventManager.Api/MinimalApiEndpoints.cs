using EventManager.Api.Auth.Authentication;
using EventManager.Domain.Events.Constants;
using EventManager.Domain.Events.Interfaces;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EventManager.Api;

public static class MinimalApiEndpoints
{
    public static WebApplication MapMinimalApiEndpoints(this WebApplication app)
    {
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
