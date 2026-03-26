using FluentValidation;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Validators.Announcement;

public class AnnouncementCreateDtoValidator : AbstractValidator<AnnouncementCreateDto>
{
    public AnnouncementCreateDtoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Bericht is verplicht.")
            .MaximumLength(500).WithMessage("Bericht mag maximaal 500 tekens bevatten.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order moet groter of gelijk aan 0 zijn.");

        When(x => x.IsScheduled, () =>
        {
            RuleFor(x => x.PlannedFrom)
                .NotNull().WithMessage("Bij inplannen moet 'Gepland Vanaf' gevuld zijn.");
            RuleFor(x => x.PlannedTill)
                .NotNull().WithMessage("Bij inplannen moet 'Gepland Tot' gevuld zijn.");
            RuleFor(x => x)
                .Must(x => x.PlannedFrom <= x.PlannedTill)
                .WithMessage("'Gepland Vanaf' moet vóór of gelijk zijn aan 'Gepland Tot'.");
        });
    }
}

