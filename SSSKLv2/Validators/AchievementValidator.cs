using FluentValidation;
using SSSKLv2.Data;

namespace SSSKLv2.Validators;

public class AchievementValidator : AbstractValidator<Achievement>
{
    public AchievementValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .WithMessage("Naam is verplicht.")
            .NotEmpty()
            .WithMessage("Naam mag niet leeg zijn.")
            .MaximumLength(30)
            .WithMessage("Naam mag maximaal 30 tekens bevatten.");
        
        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("Beschrijving is verplicht.")
            .NotEmpty()
            .WithMessage("Beschrijving mag niet leeg zijn.")
            .MaximumLength(300)
            .WithMessage("Beschrijving mag maximaal 300 tekens bevatten.");
    }
}