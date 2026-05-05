using EventManager.Domain.Entities;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using EventManager.InfrastructureTests.Fixtures;

using FluentAssertions;
using Microsoft.Extensions.Options;

namespace EventManager.InfrastructureTests.Repositories;

/// <summary>
/// Infrastructure tests for MongoDbCommentRepository.
/// Runs against a real MongoDB instance started by Testcontainers.
/// One container is shared across all tests in this class (IClassFixture).
/// </summary>
public class MongoDbCommentRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbCommentRepository _sut;

    public MongoDbCommentRepositoryTests(MongoDbFixture fixture)
    {
        var options = Options.Create(new MongoDbOptions
        {
            DatabaseName     = "infrastructure_tests",
            ConnectionString = fixture.Client.Settings.Server.ToString()
        });

        _sut = new MongoDbCommentRepository(fixture.Client, options);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidComment_ReturnsNonEmptyId()
    {
        var comment = BuildComment(Guid.NewGuid(), "Super concert !");

        var id = await _sut.CreateAsync(comment);

        id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_ValidComment_CanBeRetrievedByEventId()
    {
        var eventId = Guid.NewGuid();
        var comment = BuildComment(eventId, "Magnifique soirée");

        await _sut.CreateAsync(comment);

        var results = await _sut.GetByEventIdAsync(eventId);
        results.Should().ContainSingle(c => c.Text == "Magnifique soirée");
    }

    [Fact]
    public async Task CreateAsync_SetsIdOnComment()
    {
        var comment = BuildComment(Guid.NewGuid(), "Commentaire test");

        var id = await _sut.CreateAsync(comment);

        comment.Id.Should().Be(id);
        comment.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_NullText_StoresSuccessfully()
    {
        var eventId = Guid.NewGuid();
        var comment = BuildComment(eventId, null);

        var id = await _sut.CreateAsync(comment);
        var results = await _sut.GetByEventIdAsync(eventId);

        id.Should().NotBeNullOrEmpty();
        results.Should().ContainSingle(c => c.Text == null);
    }

    // ── GetByEventIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEventIdAsync_UnknownEventId_ReturnsEmpty()
    {
        var results = await _sut.GetByEventIdAsync(Guid.NewGuid());

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByEventIdAsync_OnlyReturnsCommentsForRequestedEvent()
    {
        var targetEventId = Guid.NewGuid();
        var otherEventId  = Guid.NewGuid();

        await _sut.CreateAsync(BuildComment(targetEventId, "Pour cet événement"));
        await _sut.CreateAsync(BuildComment(otherEventId,  "Pour un autre événement"));

        var results = await _sut.GetByEventIdAsync(targetEventId);

        results.Should().OnlyContain(c => c.EventId == targetEventId);
    }

    [Fact]
    public async Task GetByEventIdAsync_MultipleComments_ReturnedInDescendingOrder()
    {
        var eventId = Guid.NewGuid();
        var older = BuildComment(eventId, "Ancien commentaire", createdAt: DateTime.UtcNow.AddHours(-2));
        var newer = BuildComment(eventId, "Nouveau commentaire", createdAt: DateTime.UtcNow);

        await _sut.CreateAsync(older);
        await _sut.CreateAsync(newer);

        var results = (await _sut.GetByEventIdAsync(eventId)).ToList();

        results.Should().HaveCountGreaterThanOrEqualTo(2);
        results[0].CreatedAt.Should().BeOnOrAfter(results[1].CreatedAt);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EventComment BuildComment(Guid eventId, string? text, DateTime? createdAt = null) => new()
    {
        EventId   = eventId,
        UserId    = Guid.NewGuid(),
        UserName  = "TestUser",
        Text      = text,
        Rating    = 4,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };
}
