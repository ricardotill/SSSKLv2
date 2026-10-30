namespace SSSKL.Web.Registrations;

public static class CorsRegistrations
{
    public const string FrontendPolicy = "AllowFrontend";

    public static IServiceCollection AddCorsRegistrations(this IServiceCollection services, string websiteDomain)
    {
        string[] frontendOrigins =
        [
            "http://localhost:3000",
            "https://localhost:3000",
            "http://localhost:4200",
            "https://localhost:4200",
            "http://localhost:5173",
            "https://localhost:5173",
            "https://sssklv2-app.azurewebsites.net",
            $"https://{websiteDomain}",
            "https://ambitious-desert-07f83a203.2.azurestaticapps.net",
            "https://ssskl.ricardotill.nl"
        ];

        return services.AddCors(options =>
        {
            options.AddPolicy(FrontendPolicy, policy =>
            {
                policy.WithOrigins(frontendOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
    }
}
