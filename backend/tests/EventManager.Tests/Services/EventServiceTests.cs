using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;
using EventManager.Domain.Services;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManager.UnitTests.Services;

/// <summary>
/// Test class for EventService
/// </summary>
public class EventServiceTests
{
    private readonly Mock<IEventRepository>    _repositoryMock = new();
    private readonly Mock<IEventSearchService> _searchMock     = new();
    private readonly Mock<ILogger<EventService>> _loggerMock   = new();
    private readonly EventService _sut;

    public EventServiceTests()
    {
        // IndexAsync succeeds by default — individual tests override when needed.
        _searchMock
            .Setup(s => s.IndexAsync(It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        _sut = new EventService(
            _repositoryMock.Object,
            _searchMock.Object,
            _loggerMock.Object);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedDtos_WhenEventsExist()
    {
        // Arrange
        var events = new List<Event>
        {
            BuildEvent(Guid.NewGuid(), "Concert Jazz", "Concert"),
            BuildEvent(Guid.NewGuid(), "Exposition Picasso", "Exposition")
        };
        _repositoryMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync(events);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(e => e.Title == "Concert Jazz");
        result.Should().ContainSingle(e => e.Title == "Exposition Picasso");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoEventsExist()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync([]);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassPaginationParams_ToRepository()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(3, 10)).ReturnsAsync([]);

        // Act
        await _sut.GetAllAsync(page: 3, pageSize: 10);

        // Assert
        _repositoryMock.Verify(r => r.GetAllAsync(3, 10), Times.Once);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenEventExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var @event = BuildEvent(id, "Théâtre du Soleil", "Théâtre");
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(@event);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Id.Should().Be(id);
        result.Title.Should().Be("Théâtre du Soleil");
        result.Category.Should().Be("Théâtre");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Event?)null);

        // Act
        var act = async () => await _sut.GetByIdAsync(id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Event*{id}*");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldMapArtistName_WhenSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var @event = BuildEvent(id, "Rock Festival", "Concert");
        @event.ArtistName = "The Rolling Stones";
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(@event);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.ArtistName.Should().Be("The Rolling Stones");
    }



    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnDtoWithGeneratedId()
    {
        // Arrange
        var generatedId = Guid.NewGuid();
        var request = BuildCreateRequest("Nouveau Concert", "Concert");
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(generatedId);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Id.Should().Be(generatedId);
        result.Title.Should().Be("Nouveau Concert");
    }

    [Fact]
    public async Task CreateAsync_ShouldCallRepository_ExactlyOnce()
    {
        // Arrange
        var request = BuildCreateRequest("Test", "Autre");
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Event>())).ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.CreateAsync(request);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Event>()), Times.Once);
    }



    [Fact]
    public async Task CreateAsync_ShouldPassAllFieldsToRepository()
    {
        // Arrange
        var request = new CreateEventInput(
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

        // Act
        await _sut.CreateAsync(request);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(It.Is<Event>(e =>
            e.Title == "Jazz Festival" &&
            e.Location == "Paris, Olympia" &&
            e.Capacity == 500 &&
            e.Price == 35.00m &&
            e.ArtistName == "Miles Davis Tribute"
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallIndexAsync_WithCreatedEvent()
    {
        var generatedId = Guid.NewGuid();
        var request = BuildCreateRequest("Jazz Festival", "Concert");
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
        // TryIndexAsync swallows exceptions so a search outage never blocks event creation.
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
        var expected = new List<EventDto>
        {
            new(Guid.NewGuid(), "Concert Jazz", "Desc", DateTime.UtcNow, "Paris", 0, 25m, "Concert", null, DateTime.MinValue, null)
        };
        _searchMock.Setup(s => s.SearchAsync("jazz", 1, 20)).ReturnsAsync(expected);

        var result = await _sut.SearchAsync("jazz");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SearchAsync_ShouldPassPaginationParams_ToSearchService()
    {
        _searchMock.Setup(s => s.SearchAsync("rock", 2, 5)).ReturnsAsync([]);

        await _sut.SearchAsync("rock", page: 2, pageSize: 5);

        _searchMock.Verify(s => s.SearchAsync("rock", 2, 5), Times.Once);
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

    private static CreateEventInput BuildCreateRequest(string title, string category) => new(
        Title:       title,
        Description: "Description de test",
        Date:        DateTime.UtcNow.AddDays(10),
        Capacity:    100,
        Location:    "Bercy (Paris)",
        Price:       20m,
        Category:    category,
        ArtistName: "Artiste de test"
    );
}
