using FluentValidation;
using SSSKL.Contracts.Dto;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Validators.Achievement;

public class AchievementUploadValidator : AbstractValidator<AchievementDto>
{
    public AchievementUploadValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .WithMessage("Naam is verplicht.")
            .NotEmpty()
            .WithMessage("Naam mag niet leeg zijn.")
            .MaximumLength(30)
            .WithMessage("Naam mag maximaal 30 tekens bevatten.");
        
        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("Beschrijving is verplicht.")
            .NotEmpty()
            .WithMessage("Beschrijving mag niet leeg zijn.")
            .MaximumLength(300)
            .WithMessage("Beschrijving mag maximaal 300 tekens bevatten.");
    }
}