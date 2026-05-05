using EventManager.Domain.Entities;
using EventManager.Infrastructure.Search;
using EventManager.InfrastructureTests.Fixtures;
using FluentAssertions;

namespace EventManagement.InfrastructureTests.Search;

public class EventSearchServiceTests : IClassFixture<ElasticsearchFixture>, IAsyncLifetime
{
    private readonly EventSearchService _sut;
    private readonly ElasticsearchFixture _fixture;

    public EventSearchServiceTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
        _sut = new EventSearchService(fixture.Client);
    }

    public async ValueTask InitializeAsync()
    {
        // Delete the index before each test to ensure isolation across tests sharing the same container.
        try { await _fixture.Client.Indices.DeleteAsync("events", TestContext.Current.CancellationToken); }
        catch { /* index does not exist yet on first test — that's fine */ }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task IndexAsync_AndSearchAsync_ShouldReturnIndexedEvent()
    {
        var @event = BuildEvent("Jazz Festival Paris");
        await _sut.IndexAsync(@event);
        await _fixture.Client.Indices.RefreshAsync("events", TestContext.Current.CancellationToken);

        var results = await _sut.SearchAsync("Jazz");

        results.Should().ContainSingle(e => e.Title == "Jazz Festival Paris");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEvent_FromIndex()
    {
        var @event = BuildEvent("Concert à supprimer");
        await _sut.IndexAsync(@event);
        await _fixture.Client.Indices.RefreshAsync("events", TestContext.Current.CancellationToken);

        await _sut.DeleteAsync(@event.Id);
        await _fixture.Client.Indices.RefreshAsync("events", TestContext.Current.CancellationToken);

        var results = await _sut.SearchAsync("Concert à supprimer");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ReindexAllAsync_ShouldReplaceIndex()
    {
        var events = new[]
        {
            BuildEvent("Théâtre du Soleil"),
            BuildEvent("Opéra de Paris")
        };

        await _sut.ReindexAllAsync(events);
        await _fixture.Client.Indices.RefreshAsync("events", TestContext.Current.CancellationToken);

        var results = await _sut.SearchAsync("Paris");
        results.Should().ContainSingle(e => e.Title == "Opéra de Paris");
    }

    private static Event BuildEvent(string title) => new()
    {
        Id          = Guid.NewGuid(),
        Title       = title,
        Description = "Description de test",
        Date        = DateTime.UtcNow.AddDays(10),
        Location    = "Paris",
        Capacity    = 100,
        Price       = 20m,
        Category    = "Concert"
    };
}
