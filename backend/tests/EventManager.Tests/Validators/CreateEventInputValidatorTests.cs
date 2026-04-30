using EventManager.Api.Validators;
using EventManager.Domain.Constants;
using EventManager.Domain.DTOs;
using FluentValidation.TestHelper;

namespace EventManager.UnitTests.Validators;

/// <summary>
/// Test class for CreateEventInputValidator, ensuring that all validation rules for creating an event are correctly 
/// enforced according to the specifications in ##BR1: Event in ../docs/specification.md.
/// </summary>
public class CreateEventInputValidatorTests
{
    private readonly CreateEventInputValidator _sut = new();

    private static CreateEventInput Valid() => new(
        Title:       "Concert Jazz",
        Description: "Une belle soirée de jazz au Théâtre des Arts.",
        Date:        DateTime.UtcNow.Date.AddDays(1),
        Location:    "Palais des congrès (Paris)",
        Capacity:    100,
        Price:       25.00m,
        Category:    "Concert",
        ArtistName: "John Doe Quartet"
    );

    // ── Title ─────────────────────────────────────────────────────────────

    [Fact]
    public void Title_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { Title = "" })
               .ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void Title_ExceedsMaxLength_ShouldFail()
        => _sut.TestValidate(Valid() with { Title = new string('a', 201) })
               .ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void Title_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Title);

    // ── Description ───────────────────────────────────────────────────────

    [Fact]
    public void Description_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { Description = "" })
               .ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void Description_ExceedsMaxLength_ShouldFail()
        => _sut.TestValidate(Valid() with { Description = new string('a', 2001) })
               .ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void Description_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Description);

    // ── Date ──────────────────────────────────────────────────────────────

    [Fact]
    public void Date_InThePast_ShouldFail()
        => _sut.TestValidate(Valid() with { Date = DateTime.UtcNow.Date.AddDays(-1) })
               .ShouldHaveValidationErrorFor(x => x.Date);

    [Fact]
    public void Date_Today_ShouldPass()
        => _sut.TestValidate(Valid() with { Date = DateTime.UtcNow.Date })
               .ShouldNotHaveValidationErrorFor(x => x.Date);

    [Fact]
    public void Date_InTheFuture_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Date);

    // ── Capacity ──────────────────────────────────────────────────────────

    [Fact]
    public void Capacity_Zero_ShouldFail()
        => _sut.TestValidate(Valid() with { Capacity = 0 })
               .ShouldHaveValidationErrorFor(x => x.Capacity);

    [Fact]
    public void Capacity_Negative_ShouldFail()
        => _sut.TestValidate(Valid() with { Capacity = -1 })
               .ShouldHaveValidationErrorFor(x => x.Capacity);

    [Fact]
    public void Capacity_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Capacity);

    // ── Price ─────────────────────────────────────────────────────────────

    [Fact]
    public void Price_Negative_ShouldFail()
        => _sut.TestValidate(Valid() with { Price = -0.01m })
               .ShouldHaveValidationErrorFor(x => x.Price);

    [Fact]
    public void Price_Zero_ShouldPass()
        => _sut.TestValidate(Valid() with { Price = 0m })
               .ShouldNotHaveValidationErrorFor(x => x.Price);

    [Fact]
    public void Price_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Price);

    // ── Category ──────────────────────────────────────────────────────────

    [Fact]
    public void Category_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { Category = "" })
               .ShouldHaveValidationErrorFor(x => x.Category);

    [Fact]
    public void Category_Invalid_ShouldFail()
        => _sut.TestValidate(Valid() with { Category = "Cinéma" })
               .ShouldHaveValidationErrorFor(x => x.Category);

    public static IEnumerable<object[]> ValidCategoryData =>
        EventCategories.All.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(ValidCategoryData))]
    public void Category_AllValidValues_ShouldPass(string category)
        => _sut.TestValidate(Valid() with { Category = category })
               .ShouldNotHaveValidationErrorFor(x => x.Category);
}
