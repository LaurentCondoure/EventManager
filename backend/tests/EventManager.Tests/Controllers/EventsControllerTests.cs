using EventManager.Api.Controllers;
using EventManager.Domain.DTOs;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Interfaces;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManager.UnitTests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventService>              _serviceMock = new();
    private readonly Mock<ILogger<EventsController>> _loggerMock  = new();
    private readonly EventsController _sut;

    public EventsControllerTests()
    {
        _sut = new EventsController(_serviceMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithEvents()
    {
        var events = new List<EventDto>
        {
            BuildEventDto(Guid.NewGuid(), "Concert Jazz"),
            BuildEventDto(Guid.NewGuid(), "Théâtre du Soleil")
        };
        _serviceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(events);

        var result = await _sut.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(events);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WhenNoEvents()
    {
        _serviceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync([]);

        var result = await _sut.GetAll();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ShouldPassPaginationParams_ToService()
    {
        _serviceMock.Setup(s => s.GetAllAsync(3, 10)).ReturnsAsync([]);

        await _sut.GetAll(page: 3, pageSize: 10);

        _serviceMock.Verify(s => s.GetAllAsync(3, 10), Times.Once);
    }

    [Fact]
    public async Task GetAll_ShouldSetCacheControlHeader()
    {
        _serviceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync([]);

        await _sut.GetAll();

        _sut.Response.Headers.CacheControl.ToString().Should().Contain("public");
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ShouldReturnOk_WithEvent()
    {
        var id     = Guid.NewGuid();
        var @event = BuildEventDto(id, "Exposition Picasso");
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(@event);

        var result = await _sut.GetById(id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task GetById_ShouldSetCacheControlHeader()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(BuildEventDto(id, "Test"));

        await _sut.GetById(id);

        _sut.Response.Headers.CacheControl.ToString().Should().Contain("public");
    }

    [Fact]
    public async Task GetById_ShouldPropagateNotFoundException_WhenEventDoesNotExist()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id))
            .ThrowsAsync(new NotFoundException(nameof(EventDto), id));

        var act = async () => await _sut.GetById(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        var id      = Guid.NewGuid();
        var request = BuildCreateRequest("Nouveau Concert");
        var created = BuildEventDto(id, "Nouveau Concert");
        _serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(created);

        var result = await _sut.Create(request);

        var createdAt = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.ActionName.Should().Be(nameof(_sut.GetById));
        createdAt.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WithCorrectRouteId()
    {
        var id      = Guid.NewGuid();
        var request = BuildCreateRequest("Test");
        _serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(BuildEventDto(id, "Test"));

        var result = await _sut.Create(request);

        var createdAt = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.RouteValues.Should().ContainKey("id")
            .WhoseValue.Should().Be(id);
    }

    [Fact]
    public async Task Create_ShouldDelegateToService_ExactlyOnce()
    {
        var request = BuildCreateRequest("Test");
        _serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(BuildEventDto(Guid.NewGuid(), "Test"));

        await _sut.Create(request);

        _serviceMock.Verify(s => s.CreateAsync(request), Times.Once);
    }

    // ── Search ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ShouldReturnOk_WithResults()
    {
        var results = new List<SearchResultDto>
        {
            new(Guid.NewGuid(), "Jazz Festival", "Desc", DateTime.UtcNow, "Paris", 25m, "Concert", null)
        };
        _serviceMock.Setup(s => s.SearchAsync("jazz", 1, 20)).ReturnsAsync(results);

        var result = await _sut.Search("jazz");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(results);
    }

    [Fact]
    public async Task Search_ShouldReturnOk_WhenNoResults()
    {
        _serviceMock.Setup(s => s.SearchAsync("xyz", 1, 20)).ReturnsAsync([]);

        var result = await _sut.Search("xyz");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Search_ShouldPassPaginationParams_ToService()
    {
        _serviceMock.Setup(s => s.SearchAsync("rock", 2, 5)).ReturnsAsync([]);

        await _sut.Search("rock", page: 2, pageSize: 5);

        _serviceMock.Verify(s => s.SearchAsync("rock", 2, 5), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EventDto BuildEventDto(Guid id, string title) => new(
        id, title, "Description de test", DateTime.UtcNow.AddDays(10),
        "Paris, Olympia", 100, 20m, "Concert", null, DateTime.UtcNow, null);

    private static CreateEventInput BuildCreateRequest(string title) => new(
        title, "Description de test", DateTime.UtcNow.AddDays(10),
        "Paris, Olympia", 100, 20m, "Concert", null);
}
