using EventManager.Domain.Entities;
using EventManager.Infrastructure.Factories;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using EventManager.InfrastructureTests.Fixtures;

using FluentAssertions;
using Microsoft.Extensions.Options;

namespace EventManager.InfrastructureTests.Repositories;

/// <summary>
/// Infrastructure tests for SqlServerEventRepository.
/// Runs against a real SQL Server instance started by Testcontainers.
/// One container is shared across all tests in this class (IClassFixture).
/// </summary>
public class SqlServerEventRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerEventRepository _sut;

    public SqlServerEventRepositoryTests(SqlServerFixture fixture)
    {
        var factory = new DbConnectionFactory(
            Options.Create(new DatabaseOptions { DefaultConnection = fixture.ConnectionString }));

        _sut = new SqlServerEventRepository(factory);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEvent()
    {
        var id = await _sut.CreateAsync(BuildEvent("Jazz Festival"));

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Title.Should().Be("Jazz Festival");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_MapsAllColumns()
    {
        var @event = BuildEvent("Théâtre du Soleil", artistName: "Troupe du Soleil");
        var id = await _sut.CreateAsync(@event);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Description.Should().Be(@event.Description);
        result.Location.Should().Be(@event.Location);
        result.Capacity.Should().Be(@event.Capacity);
        result.Price.Should().Be(@event.Price);
        result.Category.Should().Be(@event.Category);
        result.ArtistName.Should().Be("Troupe du Soleil");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_NullArtistName_MapsCorrectly()
    {
        var id = await _sut.CreateAsync(BuildEvent("Exposition Picasso", artistName: null));

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.ArtistName.Should().BeNull();
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ContainsInsertedEvent()
    {
        var @event = BuildEvent("Expo Art Moderne");
        var id = await _sut.CreateAsync(@event);

        var results = await _sut.GetAllAsync(pageSize: 100);

        results.Should().Contain(e => e.Id == id);
    }

    [Fact]
    public async Task GetAllAsync_PageSize1_ReturnsSingleEvent()
    {
        await _sut.CreateAsync(BuildEvent("Event Alpha"));

        var results = await _sut.GetAllAsync(page: 1, pageSize: 1);

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_Page2Size1_ReturnsDifferentEventThanPage1()
    {
        await _sut.CreateAsync(BuildEvent("Pagination A", daysFromNow: 15));
        await _sut.CreateAsync(BuildEvent("Pagination B", daysFromNow: 20));

        var page1 = (await _sut.GetAllAsync(page: 1, pageSize: 1)).ToList();
        var page2 = (await _sut.GetAllAsync(page: 2, pageSize: 1)).ToList();

        page1.Should().HaveCount(1);
        page2.Should().HaveCount(1);
        page1[0].Id.Should().NotBe(page2[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_OnlyReturnsUpcomingEvents()
    {
        var allResults = await _sut.GetAllAsync(pageSize: 100);

        allResults.Should().OnlyContain(e => e.Date >= DateTime.UtcNow);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidEvent_ReturnsNonEmptyGuid()
    {
        var id = await _sut.CreateAsync(BuildEvent("New Concert"));

        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_ValidEvent_CanBeRetrievedById()
    {
        var @event = BuildEvent("Rock Night", artistName: "The Rolling Stones");

        var id = await _sut.CreateAsync(@event);
        var retrieved = await _sut.GetByIdAsync(id);

        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Rock Night");
        retrieved.ArtistName.Should().Be("The Rolling Stones");
        retrieved.Category.Should().Be(@event.Category);
        retrieved.Capacity.Should().Be(@event.Capacity);
        retrieved.Price.Should().Be(@event.Price);
    }

    [Fact]
    public async Task CreateAsync_TwoEvents_ReturnDistinctIds()
    {
        var id1 = await _sut.CreateAsync(BuildEvent("Concert A"));
        var id2 = await _sut.CreateAsync(BuildEvent("Concert B"));

        id1.Should().NotBe(id2);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingEvent_PersistsAllFields()
    {
        var id = await _sut.CreateAsync(BuildEvent("Original"));

        var @event = await _sut.GetByIdAsync(id);
        @event!.Title       = "Updated Title";
        @event.Description  = "Updated Description";
        @event.Location     = "Marseille";
        @event.Capacity     = 500;
        @event.Price        = 99m;
        @event.Category     = "Théâtre";
        @event.ArtistName   = "Nouveau Artiste";
        @event.UpdatedAt    = DateTime.UtcNow;

        await _sut.UpdateAsync(@event);

        var result = await _sut.GetByIdAsync(id);
        result!.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated Description");
        result.Location.Should().Be("Marseille");
        result.Capacity.Should().Be(500);
        result.Price.Should().Be(99m);
        result.Category.Should().Be("Théâtre");
        result.ArtistName.Should().Be("Nouveau Artiste");
        result.UpdatedAt.Should().NotBeNull();
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEvent_RemovesFromDatabase()
    {
        var id = await _sut.CreateAsync(BuildEvent("To Delete"));

        await _sut.DeleteAsync(id);

        var result = await _sut.GetByIdAsync(id);
        result.Should().BeNull();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Event BuildEvent(string title, string? artistName = null, int daysFromNow = 30) => new()
    {
        Title       = title,
        Description = "Description de test pour l'infrastructure",
        Date        = DateTime.UtcNow.AddDays(daysFromNow),
        Location    = "Paris, Bercy",
        Capacity    = 200,
        Price       = 30m,
        Category    = "Concert",
        ArtistName  = artistName,
        CreatedAt   = DateTime.UtcNow
    };
}
