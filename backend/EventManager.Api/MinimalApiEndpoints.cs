using EventManager.Domain.Constants;
using EventManager.Domain.Interfaces;

namespace EventManager.Api;

public static class MinimalApiEndpoints
{
    public static WebApplication MapMinimalApiEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
           .WithTags("Health")
           .WithSummary("Health check")
           .RequireRateLimiting("fixed");

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
