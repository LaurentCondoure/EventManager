using EventManager.Domain.DTOs;
using FluentValidation;

namespace EventManager.Api.Validators;

/// <summary>Validates the payload used to create a new event, enforcing business rules RG1.</summary>
public class CreateEventInputValidator : AbstractValidator<CreateEventInput>
{
    private static readonly string[] ValidCategories =
    [
        "Concert", "Théâtre", "Exposition", "Conférence", "Spectacle", "Autre"
    ];

    /// <summary>Initializes a new instance of <see cref="CreateEventInputValidator"/> with all RG1 rules.</summary>
    public CreateEventInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Le titre est obligatoire.")
            .MaximumLength(200).WithMessage("Le titre ne peut pas dépasser 200 caractères.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La description est obligatoire.")
            .MaximumLength(2000).WithMessage("La description ne peut pas dépasser 2000 caractères.");

        //_ represents the current CreateEventInput  object being validated
        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
            .WithMessage("La date de l'événement doit être aujourd'hui ou dans le futur.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("La capacité doit être supérieure à 0.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Le prix ne peut pas être négatif.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("La catégorie est obligatoire.")
            .Must(c => ValidCategories.Contains(c))
            .WithMessage($"La catégorie doit être parmi : {string.Join(", ", ValidCategories)}.");
    }
}
