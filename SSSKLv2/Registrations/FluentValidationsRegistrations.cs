using FluentValidation;
using FluentValidation.AspNetCore;

namespace SSSKLv2.Registrations;

public static class FluentValidationsRegistrations
{
    public static IServiceCollection AddFluentValidationsRegistrations(this IServiceCollection services)
    {
        // Add FluentAssertions related registrations here if needed in the future
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}