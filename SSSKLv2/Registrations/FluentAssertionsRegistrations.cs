using FluentValidation;
using FluentValidation.AspNetCore;

namespace SSSKLv2.Registrations;

public static class FluentAssertionsRegistrations
{
    public static IServiceCollection AddFluentAssertionsRegistrations(this IServiceCollection services)
    {
        // Add FluentAssertions related registrations here if needed in the future
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

        return services;
    }
}