using FluentValidation;

namespace SSSKLv2.Validators.Announcement;

public class AnnouncementValidator : AbstractValidator<Data.Announcement>
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