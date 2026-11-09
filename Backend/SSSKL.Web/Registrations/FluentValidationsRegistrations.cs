using FluentValidation;
using FluentValidation.AspNetCore;
using SSSKL.Application;

namespace SSSKL.Web.Registrations;

public static class FluentValidationsRegistrations
{
    public static IServiceCollection AddFluentValidationsRegistrations(this IServiceCollection services)
    {
        // Add FluentAssertions related registrations here if needed in the future
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        return services;
    }
}