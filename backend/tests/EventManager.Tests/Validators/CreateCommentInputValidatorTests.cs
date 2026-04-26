using EventManager.Api.Validators;
using EventManager.Domain.DTOs;
using FluentValidation.TestHelper;

namespace EventManager.UnitTests.Validators;

/// <summary>
/// Test class for CreateCommentInputValidator, ensuring that all validation rules for creating a comment are correctly 
/// enforced according to the specifications in ##BR2: Comment in ../docs/specification.md.
/// </summary>
public class CreateCommentInputValidatorTests
{
    private readonly CreateCommentInputValidator _sut = new();

    private static CreateCommentInput Valid() => new(
        UserId:   Guid.NewGuid(),
        UserName: "Thomas",
        Text:     "Super concert !",
        Rating:   4
    );

    // ── UserId ────────────────────────────────────────────────────────────

    [Fact]
    public void UserId_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { UserId = Guid.Empty })
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void UserId_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.UserId);

    // ── UserName ──────────────────────────────────────────────────────────

    [Fact]
    public void UserName_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { UserName = "" })
               .ShouldHaveValidationErrorFor(x => x.UserName);

    [Fact]
    public void UserName_ExceedsMaxLength_ShouldFail()
        => _sut.TestValidate(Valid() with { UserName = new string('a', 101) })
               .ShouldHaveValidationErrorFor(x => x.UserName);

    [Fact]
    public void UserName_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.UserName);

    // ── Text ──────────────────────────────────────────────────────────────

    [Fact]
    public void Text_Null_ShouldPass()
        => _sut.TestValidate(Valid() with { Text = null })
               .ShouldNotHaveValidationErrorFor(x => x.Text);

    [Fact]
    public void Text_ExceedsMaxLength_ShouldFail()
        => _sut.TestValidate(Valid() with { Text = new string('a', 1001) })
               .ShouldHaveValidationErrorFor(x => x.Text);

    [Fact]
    public void Text_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Text);

    // ── Rating ────────────────────────────────────────────────────────────

    [Fact]
    public void Rating_BelowMinimum_ShouldFail()
        => _sut.TestValidate(Valid() with { Rating = 0 })
               .ShouldHaveValidationErrorFor(x => x.Rating);

    [Fact]
    public void Rating_AboveMaximum_ShouldFail()
        => _sut.TestValidate(Valid() with { Rating = 6 })
               .ShouldHaveValidationErrorFor(x => x.Rating);

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Rating_ValidRange_ShouldPass(int rating)
        => _sut.TestValidate(Valid() with { Rating = rating })
               .ShouldNotHaveValidationErrorFor(x => x.Rating);
}
