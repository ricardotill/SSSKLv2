using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Data.DAL.Exceptions;

namespace SSSKLv2.Registrations;

/// <summary>
/// Turns exceptions that escape controllers into RFC 7807 problem responses. Known domain exceptions
/// map to 4xx; anything else is logged and returned as a generic 500 that carries the trace id
/// (never the exception message) so it can be found in Application Insights.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // Non-standard but widely used status for "client closed request"
    private const int StatusClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Method} {Path} was aborted by the client", httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusClientClosedRequest;
            return true;
        }

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            BadHttpRequestException bad => (bad.StatusCode, "Bad request"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} failed with {StatusCode}: {ExceptionType}",
                httpContext.Request.Method, httpContext.Request.Path, status, exception.GetType().Name);
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
