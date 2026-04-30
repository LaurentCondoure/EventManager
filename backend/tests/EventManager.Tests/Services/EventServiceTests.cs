using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;
using EventManager.Domain.Services;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManager.UnitTests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository>      _repositoryMock = new();
    private readonly Mock<ICommentRepository>    _commentMock    = new();
    private readonly Mock<IEventSearchService>   _searchMock     = new();
    private readonly Mock<ILogger<EventService>> _loggerMock     = new();
    private readonly EventService _sut;

    public EventServiceTests()
    {
        // IndexAsync succeeds by default — individual tests override when needed.
        _searchMock
            .Setup(s => s.IndexAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        _sut = new EventService(
            _repositoryMock.Object,
            _commentMock.Object,
            _searchMock.Object,
            _loggerMock.Object);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedDtos_WhenEventsExist()
    {
        List<Event> events = new List<Event>
        {
            BuildEvent(Guid.NewGuid(), "Concert Jazz", "Concert"),
            BuildEvent(Guid.NewGuid(), "Exposition Picasso", "Exposition")
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync(events);

        IEnumerable<EventDto> result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(e => e.Title == "Concert Jazz");
        result.Should().ContainSingle(e => e.Title == "Exposition Picasso");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoEventsExist()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync([]);

        IEnumerable<EventDto> result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassPaginationParams_ToRepository()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(3, 10)).ReturnsAsync([]);

        await _sut.GetAllAsync(page: 3, pageSize: 10);

        _repositoryMock.Verify(r => r.GetAllAsync(3, 10), Times.Once);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenEventExists()
    {
        Guid id = Guid.NewGuid();
        Event @event = BuildEvent(id, "Théâtre du Soleil", "Théâtre");
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(@event);

        EventDto result = await _sut.GetByIdAsync(id);

        result.Id.Should().Be(id);
        result.Title.Should().Be("Théâtre du Soleil");
        result.Category.Should().Be("Théâtre");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Event?)null);

        var act = async () => await _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Event*{id}*");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldMapArtistName_WhenSet()
    {
        Guid id = Guid.NewGuid();
        Event @event = BuildEvent(id, "Rock Festival", "Concert");
        @event.ArtistName = "The Rolling Stones";
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(@event);

        EventDto result = await _sut.GetByIdAsync(id);

        result.ArtistName.Should().Be("The Rolling Stones");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnDtoWithGeneratedId()
    {
        Guid generatedId = Guid.NewGuid();
        CreateEventInput request = BuildCreateRequest("Nouveau Concert", "Concert");
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(generatedId);

        EventDto result = await _sut.CreateAsync(request);

        result.Id.Should().Be(generatedId);
        result.Title.Should().Be("Nouveau Concert");
    }

    [Fact]
    public async Task CreateAsync_ShouldCallRepository_ExactlyOnce()
    {
        CreateEventInput request = BuildCreateRequest("Test", "Autre");
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(Guid.NewGuid());

        await _sut.CreateAsync(request);

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldPassAllFieldsToRepository()
    {
        CreateEventInput request = new CreateEventInput(
            Title: "Jazz Festival",
            Description: "Une nuit de jazz",
            Date: DateTime.UtcNow.AddDays(30),
            Location: "Paris, Olympia",
            Capacity: 500,
            Price: 35.00m,
            Category: "Concert",
            ArtistName: "Miles Davis Tribute"
        );
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(Guid.NewGuid());

        await _sut.CreateAsync(request);

        _repositoryMock.Verify(r => r.CreateAsync(It.Is<Event>(e =>
            e.Title == "Jazz Festival" &&
            e.Location == "Paris, Olympia" &&
            e.Capacity == 500 &&
            e.Price == 35.00m &&
            e.ArtistName == "Miles Davis Tribute" &&
            e.CreatedAt != DateTime.MinValue
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallIndexAsync_WithCreatedEvent()
    {
        Guid generatedId = Guid.NewGuid();
        CreateEventInput request = BuildCreateRequest("Jazz Festival", "Concert");
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(generatedId);

        await _sut.CreateAsync(request);

        _searchMock.Verify(s => s.IndexAsync(It.Is<Event>(e =>
            e.Id    == generatedId &&
            e.Title == "Jazz Festival"
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldComplete_WhenIndexAsyncThrows()
    {
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(Guid.NewGuid());
        _searchMock
            .Setup(s => s.IndexAsync(It.IsAny<Event>()))
            .ThrowsAsync(new Exception("Elasticsearch unavailable"));

        var act = async () => await _sut.CreateAsync(BuildCreateRequest("Test", "Autre"));

        await act.Should().NotThrowAsync();
    }

    // ── SearchAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ShouldDelegateToSearchService()
    {
        List<SearchResultDto> expected = new List<SearchResultDto>
        {
            new(Guid.NewGuid(), "Concert Jazz", "Desc", DateTime.UtcNow, "Paris", 25m, "Concert", null)
        };
        _searchMock.Setup(s => s.SearchAsync("jazz", 1, 20)).ReturnsAsync(expected);

        IEnumerable<SearchResultDto> result = await _sut.SearchAsync("jazz");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SearchAsync_ShouldPassPaginationParams_ToSearchService()
    {
        _searchMock.Setup(s => s.SearchAsync("rock", 2, 5)).ReturnsAsync([]);

        await _sut.SearchAsync("rock", page: 2, pageSize: 5);

        _searchMock.Verify(s => s.SearchAsync("rock", 2, 5), Times.Once);
    }

    // ── GetCommentsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCommentsAsync_ShouldReturnMappedDtos_WhenCommentsExist()
    {
        Guid eventId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(BuildEvent(eventId, "Test", "Concert"));
        var comments = new List<EventComment>
        {
            BuildComment("id1", eventId, "Alice", 5),
            BuildComment("id2", eventId, "Bob",   3)
        };
        _commentMock.Setup(c => c.GetByEventIdAsync(eventId)).ReturnsAsync(comments);

        IEnumerable<CommentDto> result = await _sut.GetCommentsAsync(eventId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.EventId.Should().Be(eventId));
    }

    [Fact]
    public async Task GetCommentsAsync_ShouldReturnEmpty_WhenNoComments()
    {
        Guid eventId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(BuildEvent(eventId, "Test", "Concert"));
        _commentMock.Setup(c => c.GetByEventIdAsync(eventId)).ReturnsAsync([]);

        IEnumerable<CommentDto> result = await _sut.GetCommentsAsync(eventId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommentsAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        Guid eventId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        var act = async () => await _sut.GetCommentsAsync(eventId);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Event*{eventId}*");
    }

    // ── AddCommentAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_ShouldReturnDto_WithGeneratedId()
    {
        Guid eventId = Guid.NewGuid();
        var input = new CreateCommentInput(Guid.NewGuid(), "Thomas", "Excellent !", 5);
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(BuildEvent(eventId, "Test", "Concert"));
        _commentMock.Setup(c => c.CreateAsync(It.IsAny<EventComment>())).ReturnsAsync("507f1f77bcf86cd799439011");

        CommentDto result = await _sut.AddCommentAsync(eventId, input);

        result.Id.Should().Be("507f1f77bcf86cd799439011");
        result.EventId.Should().Be(eventId);
        result.UserName.Should().Be("Thomas");
        result.Rating.Should().Be(5);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        Guid eventId = Guid.NewGuid();
        var input = new CreateCommentInput(Guid.NewGuid(), "Thomas", null, 4);
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        var act = async () => await _sut.AddCommentAsync(eventId, input);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Event*{eventId}*");
    }

    [Fact]
    public async Task AddCommentAsync_ShouldSetCreatedAt()
    {
        Guid eventId = Guid.NewGuid();
        var input = new CreateCommentInput(Guid.NewGuid(), "Alice", "Bien !", 4);
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(BuildEvent(eventId, "Test", "Concert"));
        _commentMock.Setup(c => c.CreateAsync(It.IsAny<EventComment>())).ReturnsAsync("id1");

        await _sut.AddCommentAsync(eventId, input);

        _commentMock.Verify(c => c.CreateAsync(It.Is<EventComment>(e =>
            e.CreatedAt != DateTime.MinValue
        )), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldNotCallRepository_WhenEventDoesNotExist()
    {
        Guid eventId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        var act = async () => await _sut.AddCommentAsync(eventId, new CreateCommentInput(Guid.NewGuid(), "X", null, 3));
        await act.Should().ThrowAsync<NotFoundException>();

        _commentMock.Verify(c => c.CreateAsync(It.IsAny<EventComment>()), Times.Never);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Event BuildEvent(Guid id, string title, string category) => new()
    {
        Id          = id,
        Title       = title,
        Description = "Description de test",
        Date        = DateTime.UtcNow.AddDays(10),
        Capacity    = 100,
        Price       = 20m,
        Category    = category,
        CreatedAt   = DateTime.UtcNow
    };

    private static EventComment BuildComment(string id, Guid eventId, string userName, int rating) => new()
    {
        Id        = id,
        EventId   = eventId,
        UserId    = Guid.NewGuid(),
        UserName  = userName,
        Rating    = rating,
        CreatedAt = DateTime.UtcNow
    };

    private static CreateEventInput BuildCreateRequest(string title, string category) => new(
        Title:       title,
        Description: "Description de test",
        Date:        DateTime.UtcNow.AddDays(10),
        Capacity:    100,
        Location:    "Bercy (Paris)",
        Price:       20m,
        Category:    category,
        ArtistName:  "Artiste de test"
    );
}
