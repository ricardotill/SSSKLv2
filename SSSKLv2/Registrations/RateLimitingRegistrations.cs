using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace SSSKLv2.Registrations;

public static class RateLimitingRegistrations
{
    // Anonymous identity endpoints that are worth brute-forcing or that send email.
    // Limits are per client IP and deliberately generous: the whole club often sits behind one NAT address.
    private static readonly string[] SignInPaths =
    [
        "/api/v1/identity/login",
        "/api/v1/identity/resetPassword",
        "/api/v1/identity/PasskeyAssertion",
    ];

    private static readonly string[] EmailPaths =
    [
        "/api/v1/identity/register",
        "/api/v1/identity/forgotPassword",
        "/api/v1/identity/resendConfirmationEmail",
    ];

    public static IServiceCollection AddRateLimitingRegistrations(this IServiceCollection services)
    {
        // Behind App Service / Caddy the client IP arrives in X-Forwarded-For. The proxy addresses are not
        // fixed, so trust the header but only its last hop (ForwardLimit = 1), which the proxy itself appends.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimitingRegistrations));
                logger.LogWarning("Rate limit hit for {Path} from {RemoteIp}",
                    context.HttpContext.Request.Path, context.HttpContext.Connection.RemoteIpAddress);
                return ValueTask.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var path = context.Request.Path;
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                if (Matches(path, SignInPaths))
                {
                    return RateLimitPartition.GetFixedWindowLimiter("signin:" + ip, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
                }

                if (Matches(path, EmailPaths))
                {
                    return RateLimitPartition.GetFixedWindowLimiter("email:" + ip, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    });
                }

                return RateLimitPartition.GetNoLimiter("none");
            });
        });

        return services;
    }

    private static bool Matches(PathString path, string[] candidates) =>
        candidates.Any(c => path.Equals(c, StringComparison.OrdinalIgnoreCase));
}
