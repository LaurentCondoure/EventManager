using EventManager.Api.Controllers;
using EventManager.Domain.DTOs;
using EventManager.Domain.Interfaces;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManager.UnitTests.Controllers;

public class CommentsControllerTests
{
    private readonly Mock<ICommentService>              _serviceMock = new();
    private readonly Mock<ILogger<CommentsController>> _loggerMock  = new();
    private readonly CommentsController _sut;

    public CommentsControllerTests()
    {
        _sut = new CommentsController(_serviceMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ── GetByEvent ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEvent_ShouldReturnOk_WithComments()
    {
        var eventId  = Guid.NewGuid();
        var comments = new List<CommentDto>
        {
            new("id1", eventId, Guid.NewGuid(), "Alice", "Super !", 5, DateTime.UtcNow),
            new("id2", eventId, Guid.NewGuid(), "Bob",   null,      3, DateTime.UtcNow)
        };
        _serviceMock.Setup(s => s.GetCommentsByEventId(eventId)).ReturnsAsync(comments);

        var result = await _sut.GetByEvent(eventId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(comments);
    }

    [Fact]
    public async Task GetByEvent_ShouldReturnOk_WhenNoComments()
    {
        var eventId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetCommentsByEventId(eventId)).ReturnsAsync([]);

        var result = await _sut.GetByEvent(eventId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByEvent_ShouldDelegateToService_WithCorrectEventId()
    {
        var eventId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetCommentsByEventId(eventId)).ReturnsAsync([]);

        await _sut.GetByEvent(eventId);

        _serviceMock.Verify(s => s.GetCommentsByEventId(eventId), Times.Once);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        var eventId = Guid.NewGuid();
        var input   = new CreateCommentInput(Guid.NewGuid(), "Thomas", "Excellent !", 5);
        var created = BuildCommentDto("507f1f77bcf86cd799439011", eventId, input);
        _serviceMock.Setup(s => s.CreateAsync(eventId, input)).ReturnsAsync(created);

        var result = await _sut.Create(eventId, input);

        var createdAt = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.ActionName.Should().Be(nameof(_sut.GetByEvent));
        createdAt.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction_WithCorrectRouteEventId()
    {
        var eventId = Guid.NewGuid();
        var input   = new CreateCommentInput(Guid.NewGuid(), "Thomas", null, 4);
        _serviceMock.Setup(s => s.CreateAsync(eventId, input))
            .ReturnsAsync(BuildCommentDto("id1", eventId, input));

        var result = await _sut.Create(eventId, input);

        var createdAt = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAt.RouteValues.Should().ContainKey("eventId")
            .WhoseValue.Should().Be(eventId);
    }

    [Fact]
    public async Task Create_ShouldDelegateToService_ExactlyOnce()
    {
        var eventId = Guid.NewGuid();
        var input   = new CreateCommentInput(Guid.NewGuid(), "Alice", "Bien !", 4);
        _serviceMock.Setup(s => s.CreateAsync(eventId, input))
            .ReturnsAsync(BuildCommentDto("id1", eventId, input));

        await _sut.Create(eventId, input);

        _serviceMock.Verify(s => s.CreateAsync(eventId, input), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CommentDto BuildCommentDto(string id, Guid eventId, CreateCommentInput input) =>
        new(id, eventId, input.UserId, input.UserName, input.Text, input.Rating, DateTime.UtcNow);
}
