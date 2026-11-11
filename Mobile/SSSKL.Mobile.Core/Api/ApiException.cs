using System.Net;

namespace SSSKL.Mobile.Core.Api;

/// <summary>A non-success API response; <see cref="Message"/> is the ProblemDetails title when there is one.</summary>
public sealed class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
