using SSSKL.Contracts.Services;


namespace SSSKL.Web.Registrations;

/// <summary>
/// Gates an endpoint behind a Turnstile token sent in the <see cref="HeaderName"/> header. The endpoint's
/// own logic is unchanged; requests without a valid token are rejected before it runs.
/// </summary>
public sealed class TurnstileEndpointFilter(string action) : IEndpointFilter
{
    public const string HeaderName = "X-Turnstile-Token";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var turnstile = httpContext.RequestServices.GetRequiredService<ITurnstileService>();
        var token = httpContext.Request.Headers[HeaderName].ToString();
        var result = await turnstile.VerifyAsync(
            token, action, httpContext.Connection.RemoteIpAddress?.ToString(), httpContext.RequestAborted);

        if (!result.Success)
        {
            return TypedResults.Problem(
                title: "Captcha verification failed",
                detail: "Complete the security check and try again.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["code"] = "turnstile_failed" });
        }

        return await next(context);
    }
}

public static class TurnstileEndpointFilterExtensions
{
    /// <param name="action">Turnstile action name; must match the action the frontend widget renders with.</param>
    public static RouteHandlerBuilder RequireTurnstile(this RouteHandlerBuilder builder, string action) =>
        builder.AddEndpointFilter(new TurnstileEndpointFilter(action));
}
