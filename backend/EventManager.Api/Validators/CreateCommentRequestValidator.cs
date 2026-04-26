using EventManager.Domain.DTOs;
using FluentValidation;

namespace EventManager.Api.Validators;

/// <summary>
/// Validate the input for creating a comment on an event, see ##BR2: Comment in ../docs/specification.md for more details.
/// </summary>
public class CreateCommentInputValidator : AbstractValidator<CreateCommentInput>
{
    public CreateCommentInputValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("L'identifiant utilisateur est obligatoire.");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Le nom d'utilisateur est obligatoire.")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères.");

        RuleFor(x => x.Text)
            .MaximumLength(1000).WithMessage("Le commentaire ne peut pas dépasser 1000 caractères.")
            .When(x => x.Text is not null);

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("La note doit être comprise entre 1 et 5.");
    }
}
