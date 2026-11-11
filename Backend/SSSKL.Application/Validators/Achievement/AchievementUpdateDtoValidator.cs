using FluentValidation;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Validators.Achievement;

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
