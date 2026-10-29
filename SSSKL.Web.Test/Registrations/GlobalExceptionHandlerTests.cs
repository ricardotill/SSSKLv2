using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SSSKL.Domain.Exceptions;
using SSSKL.Web.Registrations;

namespace SSSKL.Web.Test.Registrations;

[TestClass]
public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, HttpContext Context) Create()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/test";
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);
        return (handler, context);
    }

    private static async Task<JsonElement> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }

    [TestMethod]
    [DataRow(typeof(NotFoundException), 404)]
    [DataRow(typeof(UnauthorizedAccessException), 403)]
    [DataRow(typeof(ArgumentException), 400)]
    [DataRow(typeof(InvalidOperationException), 500)]
    public async Task TryHandleAsync_MapsExceptionToStatusCode(Type exceptionType, int expectedStatus)
    {
        var (handler, context) = Create();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "secret detail")!;

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(expectedStatus);
        context.Response.ContentType.Should().StartWith("application/problem+json");
    }

    [TestMethod]
    public async Task TryHandleAsync_ServerError_DoesNotLeakExceptionMessageAndIncludesTraceId()
    {
        var (handler, context) = Create();

        await handler.TryHandleAsync(context, new InvalidOperationException("connection string Password=hunter2"), CancellationToken.None);

        var body = await ReadBody(context);
        body.GetRawText().Should().NotContain("hunter2");
        body.GetProperty("status").GetInt32().Should().Be(500);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [TestMethod]
    public async Task TryHandleAsync_ClientAborted_Returns499WithoutBody()
    {
        var (handler, context) = Create();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        context.RequestAborted = cts.Token;

        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(499);
        context.Response.Body.Length.Should().Be(0);
    }
}
