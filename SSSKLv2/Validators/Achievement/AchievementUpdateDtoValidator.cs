using FluentValidation;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Validators.Achievement;

public class AchievementUpdateDtoValidator : AbstractValidator<AchievementUpdateDto>
{
    public AchievementUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is verplicht.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 tekens bevatten.");

        RuleFor(x => x.ComparisonValue)
            .GreaterThanOrEqualTo(0).WithMessage("Vergelijkingswaarde moet groter of gelijk aan 0 zijn.");
    }
}
