using System.Net;
using System.Net.Http.Json;

using EventManager.Domain.DTOs;
using EventManager.Domain.Entities;
using EventManager.Domain.Interfaces;
using EventManager.IntegrationTests.Fakes;

using Elastic.Clients.Elasticsearch;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;

namespace EventManager.IntegrationTests;

/// <summary>
/// Integration tests for CommentsController.
/// Boots the real API in memory via WebApplicationFactory.
/// SQL Server and MongoDB are replaced with in-memory fakes — no containers required.
/// </summary>
public class CommentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly InMemoryEventRepository   _eventRepository;
    private readonly InMemoryCommentRepository _commentRepository;
    private readonly HttpClient _client;

    public CommentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _eventRepository   = new InMemoryEventRepository();
        _commentRepository = new InMemoryCommentRepository();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEventRepository>();
                services.RemoveAll<EventManager.Infrastructure.Repositories.CachedEventRepository>();
                services.AddScoped<IEventRepository>(_ => _eventRepository);

                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer>(_ => new Mock<IConnectionMultiplexer>().Object);

                services.RemoveAll<ICommentRepository>();
                services.AddScoped<ICommentRepository>(_ => _commentRepository);

                services.RemoveAll<ElasticsearchClient>();
                services.RemoveAll<IEventSearchService>();
                services.AddScoped<IEventSearchService>(_ => new Mock<IEventSearchService>().Object);
            });
        }).CreateClient();
    }

    // ── GET /api/events/{eventId}/comments ───────────────────────────────────

    [Fact]
    public async Task GetByEvent_ShouldReturnOk_WhenEventExists()
    {
        var @event = SeedEvent();

        var response = await _client.GetAsync($"/api/events/{@event.Id}/comments", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByEvent_ShouldReturnEmptyList_WhenNoComments()
    {
        var @event = SeedEvent();

        var comments = await _client.GetFromJsonAsync<IEnumerable<CommentDto>>($"/api/events/{@event.Id}/comments", TestContext.Current.CancellationToken);

        comments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByEvent_ShouldReturnComments_WhenCommentsExist()
    {
        var @event = SeedEvent();
        SeedComment(@event.Id, "Excellent concert !");
        SeedComment(@event.Id, "Super ambiance !");

        var comments = await _client.GetFromJsonAsync<IEnumerable<CommentDto>>($"/api/events/{@event.Id}/comments", TestContext.Current.CancellationToken);

        comments.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByEvent_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}/comments", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/events/{eventId}/comments ──────────────────────────────────

    [Fact]
    public async Task Create_ShouldReturnCreated_WithValidPayload()
    {
        var @event = SeedEvent();
        var request = new CreateCommentInput(
            UserId:   Guid.NewGuid(),
            UserName: "Thomas",
            Text:     "Soirée inoubliable !",
            Rating:   5
        );

        var response = await _client.PostAsJsonAsync($"/api/events/{@event.Id}/comments", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<CommentDto>(TestContext.Current.CancellationToken);
        dto!.UserName.Should().Be("Thomas");
        dto.Rating.Should().Be(5);
        dto.Text.Should().Be("Soirée inoubliable !");
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WithNullText()
    {
        var @event = SeedEvent();
        var request = new CreateCommentInput(
            UserId:   Guid.NewGuid(),
            UserName: "Marie",
            Text:     null,
            Rating:   3
        );

        var response = await _client.PostAsJsonAsync($"/api/events/{@event.Id}/comments", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenRatingIsZero()
    {
        var @event = SeedEvent();
        var request = new CreateCommentInput(
            UserId:   Guid.NewGuid(),
            UserName: "Thomas",
            Text:     "Commentaire",
            Rating:   0
        );

        var response = await _client.PostAsJsonAsync($"/api/events/{@event.Id}/comments", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenRatingExceedsFive()
    {
        var @event = SeedEvent();
        var request = new CreateCommentInput(
            UserId:   Guid.NewGuid(),
            UserName: "Thomas",
            Text:     "Commentaire",
            Rating:   6
        );

        var response = await _client.PostAsJsonAsync($"/api/events/{@event.Id}/comments", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenUserNameIsEmpty()
    {
        var @event = SeedEvent();
        var request = new CreateCommentInput(
            UserId:   Guid.NewGuid(),
            UserName: "",
            Text:     "Commentaire",
            Rating:   4
        );

        var response = await _client.PostAsJsonAsync($"/api/events/{@event.Id}/comments", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        var request = new CreateCommentInput(
            UserId:   Guid.NewGuid(),
            UserName: "Thomas",
            Text:     "Commentaire",
            Rating:   4
        );

        var response = await _client.PostAsJsonAsync($"/api/events/{Guid.NewGuid()}/comments", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Event SeedEvent()
    {
        var @event = new Event
        {
            Id          = Guid.NewGuid(),
            Title       = "Concert Test",
            Description = "Description de test",
            Date        = DateTime.UtcNow.AddDays(10),
            Location    = "Paris",
            Capacity    = 100,
            Price       = 20m,
            Category    = "Concert",
            CreatedAt   = DateTime.UtcNow
        };
        _eventRepository.Seed(@event);
        return @event;
    }

    private void SeedComment(Guid eventId, string text)
    {
        _commentRepository.Seed(new EventComment
        {
            EventId   = eventId,
            UserId    = Guid.NewGuid(),
            UserName  = "TestUser",
            Text      = text,
            Rating    = 4,
            CreatedAt = DateTime.UtcNow
        });
    }
}
