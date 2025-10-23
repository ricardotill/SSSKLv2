using FluentValidation;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Validators.Product;

public class ProductCreateDtoValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 tekens bevatten.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs moet groter of gelijk aan 0 zijn.")
            .Must(p => decimal.Round(p, 2) == p).WithMessage("Prijs mag maximaal 2 cijfers achter de komma hebben.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Voorraad moet groter of gelijk aan 0 zijn.");
    }
}