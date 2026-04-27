using EventManager.Domain.DTOs;
using EventManager.Domain.Entities;
using EventManager.Domain.Interfaces;
using EventManager.Domain.Services;

using FluentAssertions;

using Moq;

namespace EventManager.UnitTests.Services;

/// <summary>
/// Tests for <see cref="CommentService"/>.
/// Verifies mapping, delegation to the repository, and DTO construction.
/// </summary>
public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _repositoryMock = new();
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _sut = new CommentService(_repositoryMock.Object);
    }

    // ── GetCommentsByEventId ──────────────────────────────────────────────

    [Fact]
    public async Task GetCommentsByEventId_ShouldReturnMappedDtos()
    {
        var eventId = Guid.NewGuid();
        var comments = new List<EventComment>
        {
            new() { Id = "id1", EventId = eventId, UserId = Guid.NewGuid(), UserName = "Alice", Text = "Super !", Rating = 5, CreatedAt = DateTime.UtcNow },
            new() { Id = "id2", EventId = eventId, UserId = Guid.NewGuid(), UserName = "Bob",   Text = null,      Rating = 3, CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock
            .Setup(r => r.GetByEventIdAsync(eventId))
            .ReturnsAsync(comments);

        var result = await _sut.GetCommentsByEventId(eventId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.EventId.Should().Be(eventId));
    }

    [Fact]
    public async Task GetCommentsByEventId_ShouldReturnEmpty_WhenNoComments()
    {
        _repositoryMock
            .Setup(r => r.GetByEventIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync([]);

        var result = await _sut.GetCommentsByEventId(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldCallRepository_WithMappedComment()
    {
        var eventId = Guid.NewGuid();
        var input   = new CreateCommentInput(Guid.NewGuid(), "Thomas", "Excellent !", 5);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<EventComment>()))
            .ReturnsAsync("507f1f77bcf86cd799439011");

        await _sut.CreateAsync(eventId, input);

        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<EventComment>(c =>
                c.EventId  == eventId       &&
                c.UserId   == input.UserId  &&
                c.UserName == input.UserName &&
                c.Rating   == input.Rating)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnDto_WithGeneratedId()
    {
        const string generatedId = "507f1f77bcf86cd799439011";
        var eventId = Guid.NewGuid();
        var input   = new CreateCommentInput(Guid.NewGuid(), "Thomas", "Excellent !", 5);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<EventComment>()))
            .ReturnsAsync(generatedId);

        var result = await _sut.CreateAsync(eventId, input);

        result.Id.Should().Be(generatedId);
        result.EventId.Should().Be(eventId);
        result.UserName.Should().Be(input.UserName);
        result.Rating.Should().Be(input.Rating);
    }
}
