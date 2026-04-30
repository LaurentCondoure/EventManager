using EventManager.Domain.Constants;
using FluentAssertions;

namespace EventManager.UnitTests.Domain;

public class EventCategoriesTests
{
    [Fact]
    public void All_ShouldNotBeEmpty()
        => EventCategories.All.Should().NotBeEmpty();

    [Fact]
    public void All_ShouldContainNoDuplicates()
        => EventCategories.All.Should().OnlyHaveUniqueItems();

    [Theory]
    [InlineData("Concert")]
    [InlineData("Théâtre")]
    [InlineData("Exposition")]
    [InlineData("Conférence")]
    [InlineData("Spectacle")]
    [InlineData("Autre")]
    public void All_ShouldContainExpectedCategory(string category)
        => EventCategories.All.Should().Contain(category);
}
