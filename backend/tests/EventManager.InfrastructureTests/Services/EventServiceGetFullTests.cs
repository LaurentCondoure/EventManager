using EventManager.Domain.DTOs;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;
using EventManager.Domain.Services;

using EventManager.Infrastructure.Factories;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using EventManager.InfrastructureTests.Fixtures;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EventManagement.InfrastructureTests.Services;

/// <summary>
/// Infrastructure tests for EventService methods that coordinate SQL Server and MongoDB.
/// GetCommentsAsync and AddCommentAsync both validate the event exists (SQL) then act on MongoDB.
/// Uses real containers for both databases — no mocks for data access.
/// </summary>
public class EventServiceGetFullTests : IClassFixture<SqlServerFixture>, IClassFixture<MongoDbFixture>
{
    private readonly EventService _sut;
    private readonly SqlServerEventRepository _eventRepository;
    private readonly MongoDbCommentRepository _commentRepository;

    public EventServiceGetFullTests(SqlServerFixture sqlFixture, MongoDbFixture mongoFixture)
    {
        var connectionFactory = new DbConnectionFactory(
            Options.Create(new DatabaseOptions { DefaultConnection = sqlFixture.ConnectionString }));

        _eventRepository = new SqlServerEventRepository(connectionFactory);

        var mongoOptions = Options.Create(new MongoDbOptions
        {
            DatabaseName     = "infrastructure_service_tests",
            ConnectionString = string.Empty
        });
        _commentRepository = new MongoDbCommentRepository(mongoFixture.Client, mongoOptions);

        _sut = new EventService(
            _eventRepository,
            _commentRepository,
            new Mock<IEventSearchService>().Object,
            NullLogger<EventService>.Instance);
    }

    // ── GetCommentsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetCommentsAsync_EventNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _sut.GetCommentsAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCommentsAsync_EventExistsWithNoComments_ReturnsEmpty()
    {
        var eventId = await _eventRepository.CreateAsync(BuildEvent("Event sans commentaires"));

        var result = await _sut.GetCommentsAsync(eventId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommentsAsync_EventExistsWithComments_ReturnsAllComments()
    {
        var eventId = await _eventRepository.CreateAsync(BuildEvent("Event avec commentaires"));
        await _commentRepository.CreateAsync(BuildComment(eventId, "Excellent !", 5));
        await _commentRepository.CreateAsync(BuildComment(eventId, "Bien",         3));

        var result = (await _sut.GetCommentsAsync(eventId)).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Text == "Excellent !");
        result.Should().Contain(c => c.Text == "Bien");
    }

    [Fact]
    public async Task GetCommentsAsync_OnlyReturnsCommentsForRequestedEvent()
    {
        var eventId1 = await _eventRepository.CreateAsync(BuildEvent("Événement 1"));
        var eventId2 = await _eventRepository.CreateAsync(BuildEvent("Événement 2"));

        await _commentRepository.CreateAsync(BuildComment(eventId1, "Commentaire event 1", 4));
        await _commentRepository.CreateAsync(BuildComment(eventId2, "Commentaire event 2", 2));

        var result = (await _sut.GetCommentsAsync(eventId1)).ToList();

        result.Should().OnlyContain(c => c.EventId == eventId1);
    }

    // ── AddCommentAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_EventNotFound_ThrowsNotFoundException()
    {
        var input = new CreateCommentInput(Guid.NewGuid(), "Thomas", "Commentaire", 4);

        Func<Task> act = () => _sut.AddCommentAsync(Guid.NewGuid(), input);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddCommentAsync_ValidComment_ReturnsCommentDto()
    {
        var eventId = await _eventRepository.CreateAsync(BuildEvent("Concert pour commentaire"));
        var input   = new CreateCommentInput(Guid.NewGuid(), "Marie", "Soirée parfaite !", 5);

        var result = await _sut.AddCommentAsync(eventId, input);

        result.Should().NotBeNull();
        result.UserName.Should().Be("Marie");
        result.Rating.Should().Be(5);
        result.Text.Should().Be("Soirée parfaite !");
        result.EventId.Should().Be(eventId);
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddCommentAsync_ValidComment_CanBeRetrievedViaGetComments()
    {
        var eventId = await _eventRepository.CreateAsync(BuildEvent("Concert roundtrip"));
        var input   = new CreateCommentInput(Guid.NewGuid(), "Thomas", "Incroyable !", 5);

        await _sut.AddCommentAsync(eventId, input);

        var comments = (await _sut.GetCommentsAsync(eventId)).ToList();
        comments.Should().ContainSingle(c => c.Text == "Incroyable !" && c.UserName == "Thomas");
    }

    [Fact]
    public async Task AddCommentAsync_NullText_StoresSuccessfully()
    {
        var eventId = await _eventRepository.CreateAsync(BuildEvent("Concert note uniquement"));
        var input   = new CreateCommentInput(Guid.NewGuid(), "Anonyme", null, 3);

        var result = await _sut.AddCommentAsync(eventId, input);

        result.Text.Should().BeNull();
        result.Rating.Should().Be(3);
    }

    // ── GetWithCommentsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetWithCommentsAsync_EventNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _sut.GetWithCommentsAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetWithCommentsAsync_EventExistsWithNoComments_ReturnsEventAndEmptyComments()
    {
        var eventId = await _eventRepository.CreateAsync(BuildEvent("Concert sans commentaires"));

        var result = await _sut.GetWithCommentsAsync(eventId);

        result.Event.Id.Should().Be(eventId);
        result.Event.Title.Should().Be("Concert sans commentaires");
        result.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWithCommentsAsync_EventExistsWithComments_ReturnsEventAndAllComments()
    {
        var eventId = await _eventRepository.CreateAsync(BuildEvent("Concert avec avis"));
        await _commentRepository.CreateAsync(BuildComment(eventId, "Excellent !", 5));
        await _commentRepository.CreateAsync(BuildComment(eventId, "Bien",         3));

        var result = await _sut.GetWithCommentsAsync(eventId);

        result.Event.Id.Should().Be(eventId);
        result.Comments.Should().HaveCount(2);
        result.Comments.Should().Contain(c => c.Text == "Excellent !");
        result.Comments.Should().Contain(c => c.Text == "Bien");
    }

    [Fact]
    public async Task GetWithCommentsAsync_OnlyReturnsCommentsForRequestedEvent()
    {
        var eventId1 = await _eventRepository.CreateAsync(BuildEvent("Concert A"));
        var eventId2 = await _eventRepository.CreateAsync(BuildEvent("Concert B"));
        await _commentRepository.CreateAsync(BuildComment(eventId1, "Avis pour A", 4));
        await _commentRepository.CreateAsync(BuildComment(eventId2, "Avis pour B", 2));

        var result = await _sut.GetWithCommentsAsync(eventId1);

        result.Event.Id.Should().Be(eventId1);
        result.Comments.Should().ContainSingle()
            .Which.Text.Should().Be("Avis pour A");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Event BuildEvent(string title) => new()
    {
        Title       = title,
        Description = "Description de test infrastructure",
        Date        = DateTime.UtcNow.AddDays(30),
        Location    = "Paris, Bercy",
        Capacity    = 100,
        Price       = 20m,
        Category    = "Concert",
        CreatedAt   = DateTime.UtcNow
    };

    private static EventComment BuildComment(Guid eventId, string? text, int rating) => new()
    {
        EventId   = eventId,
        UserId    = Guid.NewGuid(),
        UserName  = "TestUser",
        Text      = text,
        Rating    = rating,
        CreatedAt = DateTime.UtcNow
    };
}
