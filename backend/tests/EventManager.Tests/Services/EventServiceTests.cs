using EventManager.Domain.Entities;
using EventManager.Domain.DTOs;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;
using EventManager.Domain.Services;

using FluentAssertions;
using Moq;

namespace EventManager.UnitTests.Services;

/// <summary>
/// Test class for EventService
/// </summary>
public class EventServiceTests
{
    /// <summary>
    /// Repository mock used to simulate data access for testing the service layer in isolation.
    /// </summary>
    private readonly Mock<IEventRepository> _repositoryMock;

    /// <summary>
    /// System Under Test: the EventService instance being tested, with the repository mock injected to control its behavior during tests.
    /// </summary>
    private readonly EventService _sut;

    /// <summary>
    /// 
    /// </summary>
    public EventServiceTests()
    {
        //Instanciate Mock ti inject into SUT
        _repositoryMock = new Mock<IEventRepository>();

        _sut = new EventService(
            _repositoryMock.Object);
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
        Price:       20m,
        Category:    category
    );
}
