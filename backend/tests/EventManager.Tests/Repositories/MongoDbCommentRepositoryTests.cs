using EventManager.Domain.Entities;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

using Moq;

namespace EventManager.UnitTests.Repositories;

/// <summary>
/// Tests for <see cref="MongoDbCommentRepository"/>.
/// Verifies filter, sort, insert behaviour and delegation to the MongoDB driver.
/// </summary>
public class MongoDbCommentRepositoryTests
{
    private readonly Mock<IMongoCollection<EventComment>> _collectionMock = new();
    private readonly MongoDbCommentRepository _sut;

    public MongoDbCommentRepositoryTests()
    {
        var databaseMock = new Mock<IMongoDatabase>();
        var clientMock   = new Mock<IMongoClient>();

        var options = Options.Create(new MongoDbOptions
        {
            DatabaseName     = "TestDB",
            ConnectionString = "mongodb://localhost:27017"
        });

        databaseMock
            .Setup(d => d.GetCollection<EventComment>("event_comments", null))
            .Returns(_collectionMock.Object);

        clientMock
            .Setup(c => c.GetDatabase("TestDB", null))
            .Returns(databaseMock.Object);

        _sut = new MongoDbCommentRepository(clientMock.Object, options);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetupInsert(string generatedId)
    {
        _collectionMock
            .Setup(c => c.InsertOneAsync(
                It.IsAny<EventComment>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventComment, InsertOneOptions, CancellationToken>(
                (comment, _, _) => comment.Id = generatedId)
            .Returns(Task.CompletedTask);
    }

    // ── GetByEventIdAsync ─────────────────────────────────────────────────

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnGeneratedId()
    {
        const string expectedId = "507f1f77bcf86cd799439011";
        var comment = new EventComment { EventId = Guid.NewGuid(), UserName = "Thomas", Rating = 4 };
        SetupInsert(expectedId);

        var result = await _sut.CreateAsync(comment);

        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallInsertOnce()
    {
        var comment = new EventComment { EventId = Guid.NewGuid(), UserName = "Thomas", Rating = 4 };
        SetupInsert("507f1f77bcf86cd799439011");

        await _sut.CreateAsync(comment);

        _collectionMock.Verify(c => c.InsertOneAsync(
            comment,
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
