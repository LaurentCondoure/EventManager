using EventManager.Domain.Constants;

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

        return app;
    }
}
