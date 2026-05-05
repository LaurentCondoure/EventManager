using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;
using EventManager.IntegrationTests.Fakes;

using System.Net;
using System.Net.Http.Json;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;
using Elastic.Clients.Elasticsearch;

namespace EventManager.IntegrationTests;

/// <summary>
/// Integration tests for EventsController.
/// Uses WebApplicationFactory to boot the real API in memory.
/// Replaces SQL Server and Redis with in-memory fakes — no running containers needed.
/// </summary>
public class EventsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly InMemoryEventRepository _repository;

    public EventsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory    = factory;
        _repository = new InMemoryEventRepository();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQL Server repositories with in-memory fake
                services.RemoveAll<IEventRepository>();
                services.RemoveAll<EventManager.Infrastructure.Repositories.CachedEventRepository>();
                services.AddScoped<IEventRepository>(_ => _repository);

                // Replace Redis with a mock
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(_ => new Mock<IConnectionMultiplexer>().Object);

                // Replace MongoDB with a mock
                services.RemoveAll<ICommentRepository>();
                services.AddScoped<ICommentRepository>(_ => new Mock<ICommentRepository>().Object);

                // Replace Elasticsearch with a mock
                services.RemoveAll<ElasticsearchClient>();
                services.RemoveAll<IEventSearchService>();
                services.AddScoped<IEventSearchService>(_ => new Mock<IEventSearchService>().Object);
            });
        }).CreateClient();
    }

    // ── GET /api/events ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/events", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoEvents()
    {
        var events = await _client.GetFromJsonAsync<IEnumerable<EventDto>>("/api/events", TestContext.Current.CancellationToken);

        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldReturnEvents_WhenEventsExist()
    {
        _repository.Seed(BuildEvent("Concert Jazz"));
        _repository.Seed(BuildEvent("Exposition Picasso"));

        var events = await _client.GetFromJsonAsync<IEnumerable<EventDto>>("/api/events", TestContext.Current.CancellationToken);

        events.Should().HaveCount(2);
    }

    // ── GET /api/events/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenEventExists()
    {
        var @event = BuildEvent("Concert Jazz");
        _repository.Seed(@event);

        var response = await _client.GetAsync($"/api/events/{@event.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<EventDto>(TestContext.Current.CancellationToken);
        dto!.Title.Should().Be("Concert Jazz");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/events ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ShouldReturnCreated_WithValidPayload()
    {
        var request = new CreateEventInput(
            Title: "Rock Festival",
            Description: "Une soirée rock inoubliable",
            Date: DateTime.UtcNow.AddDays(30),
            Location: "Paris, Zénith",
            Capacity: 500,
            Price: 35m,
            Category: "Concert",
            ArtistName: "The Rolling Stones"
        );

        var response = await _client.PostAsJsonAsync("/api/events", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<EventDto>(TestContext.Current.CancellationToken);
        dto!.Title.Should().Be("Rock Festival");
        dto.ArtistName.Should().Be("The Rolling Stones");
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenTitleIsEmpty()
    {
        var request = new CreateEventInput(
            Title: "",
            Description: "Description",
            Date: DateTime.UtcNow.AddDays(10),
            Location: "Paris",
            Capacity: 100,
            Price: 20m,
            Category: "Concert",
            ArtistName: null
        );

        var response = await _client.PostAsJsonAsync("/api/events", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenDateIsInThePast()
    {
        var request = new CreateEventInput(
            Title: "Événement passé",
            Description: "Description",
            Date: DateTime.UtcNow.AddDays(-1),
            Location: "Paris",
            Capacity: 100,
            Price: 20m,
            Category: "Concert",
            ArtistName: null
        );

        var response = await _client.PostAsJsonAsync("/api/events", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenCategoryIsInvalid()
    {
        var request = new CreateEventInput(
            Title: "Événement test",
            Description: "Description",
            Date: DateTime.UtcNow.AddDays(10),
            Location: "Paris",
            Capacity: 100,
            Price: 20m,
            Category: "InvalidCategory",
            ArtistName: null
        );

        var response = await _client.PostAsJsonAsync("/api/events", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Cache-Control headers ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ShouldSetPublicCacheControlHeader()
    {
        var response = await _client.GetAsync("/api/events", TestContext.Current.CancellationToken);

        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(300));
    }

    [Fact]
    public async Task GetById_ShouldSetPublicCacheControlHeader()
    {
        var @event = BuildEvent("Cache-Control Test");
        _repository.Seed(@event);

        var response = await _client.GetAsync($"/api/events/{@event.Id}", TestContext.Current.CancellationToken);

        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(600));
    }

    // ── GET /api/events/{id}/full ────────────────────────────────────────────

    [Fact]
    public async Task GetFull_ShouldReturnEventWithComments_WhenEventExists()
    {
        var @event = BuildEvent("Festival Jazz");
        _repository.Seed(@event);

        var comments = new[]
        {
            BuildComment(@event.Id, "Super concert"),
            BuildComment(@event.Id, "Magnifique")
        };

        var client = CreateClientWithComments(_factory, comments);
        var response = await client.GetAsync($"/api/events/{@event.Id}/full", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<EventWithCommentsDto>(TestContext.Current.CancellationToken);
        dto!.Event.Title.Should().Be("Festival Jazz");
        dto.Comments.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFull_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}/full", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private readonly WebApplicationFactory<Program> _factory;

    private HttpClient CreateClientWithComments(
        WebApplicationFactory<Program> factory,
        IEnumerable<EventComment> comments)
    {
        var commentMock = new Mock<ICommentRepository>();
        commentMock
            .Setup(r => r.GetByEventIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(comments);

        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEventRepository>();
                services.RemoveAll<EventManager.Infrastructure.Repositories.CachedEventRepository>();
                services.AddScoped<IEventRepository>(_ => _repository);

                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(_ => new Mock<IConnectionMultiplexer>().Object);

                services.RemoveAll<ICommentRepository>();
                services.AddScoped<ICommentRepository>(_ => commentMock.Object);

                services.RemoveAll<ElasticsearchClient>();
                services.RemoveAll<IEventSearchService>();
                services.AddScoped<IEventSearchService>(_ => new Mock<IEventSearchService>().Object);

            });
        }).CreateClient();
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
        Category    = "Concert",
        CreatedAt   = DateTime.UtcNow
    };

    private static EventComment BuildComment(Guid eventId, string text) => new()
    {
        Id       = Guid.NewGuid().ToString(),
        EventId  = eventId,
        UserId   = Guid.NewGuid(),
        UserName = "TestUser",
        Text     = text,
        Rating   = 4,
        CreatedAt = DateTime.UtcNow
    };
}
