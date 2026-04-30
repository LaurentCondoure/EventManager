namespace EventManager.Api;

public static class MinimalApiEndpoints
{
    public static WebApplication MapMinimalApiEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
           .WithTags("Health")
           .WithSummary("Health check")
           .RequireRateLimiting("fixed");

        return app;
    }
}
