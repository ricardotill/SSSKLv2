using FluentValidation;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Validators.Product;

public class ProductUpdateDtoValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is verplicht voor updates");

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
