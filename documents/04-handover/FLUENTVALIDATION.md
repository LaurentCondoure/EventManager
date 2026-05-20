# FluentValidation

## Principe

FluentValidation is used to separate business rules validation on the model and the controller. Each Validator has his dedicated class, testable on isolation level.

---

## Define a validator

```csharp
public class CreateEventInputValidator : AbstractValidator<CreateEventInput>
{
    public CreateEventInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Le titre est obligatoire.")
            .MaximumLength(200).WithMessage("Le titre ne peut pas dépasser 200 caractères.");

        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
            .WithMessage("La date doit être aujourd'hui ou dans le futur.");

        RuleFor(x => x.Category)
            .Must(c => EventCategories.All.Contains(c))
            .WithMessage("Catégorie invalide.");

        //Conditional rule : applied if ArtistName is not empty
        RuleFor(x => x.ArtistName)
            .MaximumLength(200).WithMessage("Le nom de l'artiste ne peut pas dépasser 200 caractères.")
            .When(x => x.ArtistName is not null);
}
```

---

## Common rules

| rule | Description |
|---|---|
| `NotEmpty()` | null non empty value |
| `MaximumLength(n)` | max lenght |
| `GreaterThan(n)` | Value strictly superior |
| `GreaterThanOrEqualTo(n)` | Value greater than or equal |
| `InclusiveBetween(min, max)` | Value in a closed interval |
| `Must(predicate)` | Lambda custom rule |
| `When(condition)` | apply rule on true condition  |

---

## ASP.NET Core integration

```csharp
// Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEventInputValidator>();
```

with `AddFluentValidationAutoValidation`, the validation is automatically processedbefore controller execution. If one rule does not matche, ASP.NET retourne retuen an `400 Bad Request` error and the controller is never called.

---

## Unit tests

`FluentValidation.TestHelper` provides expressive assertions. It no longer exists as a separate NuGet package since v9 — it is part of the main `FluentValidation` package.

In this project, no additional reference is necessary in the test project's `.csproj`: `FluentValidation` is transitively available through the following dependency chain :

```
EventManager.Tests
    └── EventManager.Api               (ProjectReference)
            └── FluentValidation.AspNetCore    (PackageReference)
                    └── FluentValidation       (NuGet package)
```

```csharp
public class CreateEventInputValidatorTests
{
    private readonly CreateEventInputValidator _sut = new();

    // Valid object used as baseline — One property is changed at the time
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

    [Fact]
    public void Title_Empty_ShouldFail()
        => _sut.TestValidate(Valid() with { Title = "" })
               .ShouldHaveValidationErrorFor(x => x.Title);

    [Fact]
    public void Title_Valid_ShouldPass()
        => _sut.TestValidate(Valid())
               .ShouldNotHaveValidationErrorFor(x => x.Title);

    public static IEnumerable<object[]> ValidCategoryData =>
        EventCategories.All.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(ValidCategoryData))]
    public void Category_ValidValues_ShouldPass(string category)
        => _sut.TestValidate(Valid() with { Category = category })
               .ShouldNotHaveValidationErrorFor(x => x.Category);
}
```

### Recommended pattern

1. Create an object `Valid()` which satisfy all rules
2. use `with { Propriété = valeur }` to test only one rule at the time
3. `ShouldHaveValidationErrorFor` — Check if an error is raised on tested property
4. `ShouldNotHaveValidationErrorFor` —  Check if no error is raised on tested property
5. `[Theory]` + `[MemberData]` generate a test case for each value (ex : xunit call `ValidCategoryData` and generate a test case for each member of `EventCategories.All` enum. If a value is added in `EventCategories.All`, the test case is automatically added and tested)
