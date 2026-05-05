using EventManager.InfrastructureTests.Fixtures;

using FluentAssertions;

namespace EventManager.InfrastructureTests.Varnish;

/// <summary>
/// Tests Varnish caching rules using real containers (nginx mock backend + Varnish).
///
/// Each test uses a unique URL so tests are order-independent regardless of xUnit parallelism:
/// the Varnish cache is shared across the fixture lifetime, but cache entries are keyed by URL.
/// </summary>
public class VarnishCacheTests : IClassFixture<VarnishFixture>
{
    private readonly HttpClient _client;

    public VarnishCacheTests(VarnishFixture fixture)
    {
        _client = fixture.HttpClient;
    }

    // ── GET /api/events — list ────────────────────────────────────────────────

    [Fact]
    public async Task EventsList_FirstRequest_ShouldBeCacheMiss()
    {
        var path = $"/api/events?_t={Guid.NewGuid():N}";

        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        XCache(response).Should().Be("MISS");
    }

    [Fact]
    public async Task EventsList_SecondRequest_ShouldBeCacheHit()
    {
        var path = $"/api/events?_t={Guid.NewGuid():N}";
        await _client.GetAsync(path, TestContext.Current.CancellationToken);

        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        XCache(response).Should().Be("HIT");
    }

    // ── GET /api/events/{id} — single event ───────────────────────────────────

    [Fact]
    public async Task SingleEvent_FirstRequest_ShouldBeCacheMiss()
    {
        var path = $"/api/events/{Guid.NewGuid()}";

        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        XCache(response).Should().Be("MISS");
    }

    [Fact]
    public async Task SingleEvent_SecondRequest_ShouldBeCacheHit()
    {
        var path = $"/api/events/{Guid.NewGuid()}";
        await _client.GetAsync(path, TestContext.Current.CancellationToken);

        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        XCache(response).Should().Be("HIT");
    }

    // ── GET /api/events/search — bypasses cache ───────────────────────────────

    [Fact]
    public async Task Search_AlwaysBypassesCache()
    {
        // Even after a first request, subsequent requests must not be served from cache.
        await _client.GetAsync("/api/events/search?q=test", TestContext.Current.CancellationToken);

        var response = await _client.GetAsync("/api/events/search?q=test", TestContext.Current.CancellationToken);

        XCache(response).Should().Be("MISS");
    }

    // ── GET /api/events/{id}/full — bypasses cache ────────────────────────────

    [Fact]
    public async Task Full_AlwaysBypassesCache()
    {
        var path = $"/api/events/{Guid.NewGuid()}/full";
        await _client.GetAsync(path, TestContext.Current.CancellationToken);

        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        XCache(response).Should().Be("MISS");
    }

    // ── POST — bypasses cache ─────────────────────────────────────────────────

    [Fact]
    public async Task Post_AlwaysBypassesCache()
    {
        // Mutations must never be served from cache.
        var response = await _client.PostAsync(
            "/api/events",
            new StringContent("{}"),
            TestContext.Current.CancellationToken);

        XCache(response).Should().Be("MISS");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string XCache(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Cache", out var values)
            ? values.Single()
            : throw new InvalidOperationException("X-Cache header is missing from the response.");
}
