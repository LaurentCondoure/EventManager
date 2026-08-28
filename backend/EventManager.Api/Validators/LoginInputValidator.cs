using EventManager.Domain.Identity.DTOs;
using FluentValidation;

namespace EventManager.Api.Validators;

/// <summary>Validates the input for <c>POST /auth/login</c>: email and password are both required.</summary>
public class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est obligatoire.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est obligatoire.");
    }
}
