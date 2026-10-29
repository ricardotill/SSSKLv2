using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.CookiePolicy;

namespace SSSKL.Web.Registrations;

public static class AntiforgeryRegistrations
{
    public static IServiceCollection AddAntiforgeryRegistrations(this IServiceCollection services, CookieSecurePolicy cookieSecurePolicy)
    {
        services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-XSRF-TOKEN";
            o.SuppressXFrameOptionsHeader = true;
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = cookieSecurePolicy;
            o.Cookie.SameSite = SameSiteMode.Lax;
        });

        // Global cookie policy to enforce HttpOnly and secure cookies where possible.
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.HttpOnly = HttpOnlyPolicy.Always;
            options.Secure = cookieSecurePolicy;
            options.MinimumSameSitePolicy = SameSiteMode.Lax;
        });

        return services;
    }

    // Issues a readable XSRF-TOKEN cookie that the frontend echoes back in the X-XSRF-TOKEN header.
    public static IApplicationBuilder UseXsrfTokenCookie(this IApplicationBuilder app, bool secureCookie)
    {
        return app.Use(async (context, next) =>
        {
            // Skip antiforgery for blob image requests to avoid Set-Cookie headers that prevent CDN caching
            if (context.Request.Path.StartsWithSegments("/api/v1/blob"))
            {
                await next(context);
                return;
            }

            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(context);

            if (tokens.RequestToken != null)
            {
                context.Response.Cookies.Append(
                    "XSRF-TOKEN",
                    tokens.RequestToken,
                    // Double-submit token: Angular must read it from JS, so it can't be HttpOnly
                    new CookieOptions // NOSONAR (S3330)
                    {
                        HttpOnly = false,
                        Secure = secureCookie,
                        SameSite = SameSiteMode.Lax
                    });
            }

            await next(context);
        });
    }
}
