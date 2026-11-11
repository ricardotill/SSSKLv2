using Microsoft.AspNetCore.Identity;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Web.Registrations;

public static class AuthenticationRegistrations
{
    private const string BearerOrCookieScheme = "BearerOrCookie";

    public static IServiceCollection AddAuthenticationRegistrations(this IServiceCollection services, CookieSecurePolicy cookieSecurePolicy, string websiteDomain)
    {
        var authBuilder = services.AddAuthentication(options =>
        {
            // Policy scheme as default: forwards to bearer when an Authorization header is present,
            // otherwise falls back to the Identity cookie handler.
            options.DefaultScheme = BearerOrCookieScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });

        authBuilder.AddIdentityCookies();
        authBuilder.AddBearerToken(IdentityConstants.BearerScheme, options =>
        {
            options.RefreshTokenExpiration = TimeSpan.FromDays(180);
        });

        // Inspect the Authorization header at runtime to pick the correct handler.
        authBuilder.AddPolicyScheme(BearerOrCookieScheme, "Bearer or Cookie", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                string? authorization = context.Request.Headers.Authorization;
                if (authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    return IdentityConstants.BearerScheme;
                return IdentityConstants.ApplicationScheme;
            };
        });

        // Application and identity-related cookies are HttpOnly and use a secure policy.
        services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(180);
            options.SlidingExpiration = false;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;

            // For API paths, return 401/403 instead of redirecting to the login page.
            options.Events.OnRedirectToLogin = context => RedirectOrStatus(context.HttpContext, context.RedirectUri, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => RedirectOrStatus(context.HttpContext, context.RedirectUri, StatusCodes.Status403Forbidden);
        });

        // External and two-factor cookies should also be HttpOnly and secure.
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Roles.Kiosk, policy => policy.RequireRole(Roles.Kiosk))
            .AddPolicy(Roles.User, policy => policy.RequireRole(Roles.User))
            .AddPolicy(Roles.Guest, policy => policy.RequireRole(Roles.Guest));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        services.Configure<IdentityPasskeyOptions>(options =>
        {
            options.ServerDomain = websiteDomain;
        });

        return services;
    }

    private static Task RedirectOrStatus(HttpContext context, string redirectUri, int apiStatusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = apiStatusCode;
            return Task.CompletedTask;
        }
        context.Response.Redirect(redirectUri);
        return Task.CompletedTask;
    }
}
