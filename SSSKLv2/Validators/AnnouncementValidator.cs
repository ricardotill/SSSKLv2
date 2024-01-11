using FluentValidation;
using SSSKLv2.Data;

namespace SSSKLv2.Validators;

public class AnnouncementValidator : AbstractValidator<Announcement>
{
    public AnnouncementValidator()
    {
        When(x => x.IsScheduled, () =>
        {
            RuleFor(x => x.PlannedFrom)
                .NotNull()
                .WithMessage("Bij inplannen moet 'Gepland Vanaf' gevuld zijn.");
            RuleFor(x => x.PlannedTill)
                .NotNull()
                .WithMessage("Bij inplannen moet 'Gepland Tot' gevuld zijn.");
        });
    }
}