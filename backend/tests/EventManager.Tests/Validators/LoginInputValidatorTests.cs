using EventManager.Api.Validators;
using EventManager.Domain.Identity.DTOs;
using FluentValidation.TestHelper;

namespace EventManager.UnitTests.Validators;

/// <summary>Ensures both fields required by <c>POST /auth/login</c> (TASK-001) are enforced.</summary>
public class LoginInputValidatorTests
{
    private readonly LoginInputValidator _sut = new();

    private static LoginInput Valid() => new(
        Email:    "marie@example.com",
        Password: "correct-password"
    );

    // ── Email ─────────────────────────────────────────────────────────────

    [Fact]
    public void Email_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { Email = "" })
               .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Email_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Email);

    // ── Password ──────────────────────────────────────────────────────────

    [Fact]
    public void Password_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { Password = "" })
               .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void Password_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Password);
}
