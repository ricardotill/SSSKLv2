using FluentValidation;
using FluentValidation.Results;
using SSSKLv2.Components.Pages;

namespace SSSKLv2.Validators;

public class BestellingDTOValidator : AbstractValidator<Home.BestellingDTO>
{
    public BestellingDTOValidator()
    {
        RuleFor(x => x.Products.Count(x => x.Selected))
            .GreaterThan(0)
            .WithMessage("Er moet minimaal 1 product geselecteerd zijn.");
        RuleFor(x => x.Users.Count(x => x.Selected))
            .GreaterThan(0)
            .WithMessage("Er moet minimaal 1 gebruiker geselecteerd zijn.");
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Hoeveelheid moet groter dan 1 zijn");
        RuleFor(x => x.CheckStock())
            .Equal(true)
            .WithMessage(x => $"Voor een van de geselecteerde producten is de voorraad te laag voor jouw bestelling.");
    }
}