using FluentValidation;
using SSSKLv2.Dto.Api;

namespace SSSKLv2.Validators.Event;

public class EventCreateDtoValidator : AbstractValidator<EventCreateDto>
{
    public EventCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Titel is verplicht.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Beschrijving is verplicht.");

        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("Startdatum is verplicht.")
            .NotEqual(default(DateTime)).WithMessage("Startdatum is verplicht.");

        RuleFor(x => x.EndDateTime)
            .NotEmpty().WithMessage("Einddatum is verplicht.")
            .NotEqual(default(DateTime)).WithMessage("Einddatum is verplicht.")
            .GreaterThanOrEqualTo(x => x.StartDateTime).WithMessage("Einddatum moet na de startdatum liggen.");
    }
}
