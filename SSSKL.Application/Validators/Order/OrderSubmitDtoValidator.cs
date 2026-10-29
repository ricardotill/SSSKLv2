using FluentValidation;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Validators.Order;

public class OrderSubmitDtoValidator : AbstractValidator<OrderSubmitDto>
{
    public OrderSubmitDtoValidator()
    {
        RuleFor(x => x.Products)
            .NotNull().WithMessage("Producten moeten worden opgegeven")
            .Must(list => list != null && list.Count > 0).WithMessage("Er moeten minimaal 1 of meer producten worden geselecteerd");

        RuleFor(x => x.Users)
            .NotNull().WithMessage("Gebruikers moeten worden opgegeven")
            .Must(list => list != null && list.Count > 0).WithMessage("Er moeten minimaal 1 of meer gebruikers worden geselecteerd");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(1).WithMessage("Aantal moet minstens 1 zijn")
            .LessThanOrEqualTo(1000).WithMessage("Aantal is onredelijk groot");

        RuleFor(x => x.Split)
            .NotNull().WithMessage("Split moet opgegeven zijn");
    }
}
