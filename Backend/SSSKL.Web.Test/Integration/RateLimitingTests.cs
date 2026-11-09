using System.Net;
using System.Net.Http.Json;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class RateLimitingTests
{
    public TestContext TestContext { get; set; } = null!;

    // Production limits: the shared factory raises them for every other test.
    private static SssklWebApplicationFactory CreateFactory() =>
        new SssklWebApplicationFactory()
            .WithSetting("RateLimiting:SignInPermitLimit", "30")
            .WithSetting("RateLimiting:EmailPermitLimit", "10");

    [TestMethod]
    public async Task ForgotPassword_AfterLimit_Returns429WithRetryAfter()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email = "nobody@example.com" }, TestContext.CancellationToken);
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode, $"request {i + 1} was limited");
        }

        var limited = await client.PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email = "nobody@example.com" }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.IsTrue(limited.Headers.Contains("Retry-After"));
    }

    [TestMethod]
    public async Task UnlistedEndpoints_AreNotLimited()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 40; i++)
        {
            var response = await client.GetAsync("/healthz", TestContext.CancellationToken);
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    [TestMethod]
    public async Task Login_RespectsConfiguredLimit()
    {
        await using var factory = new SssklWebApplicationFactory().WithSetting("RateLimiting:SignInPermitLimit", "3");
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/identity/login", new { userName = "nobody", password = "wrong" }, TestContext.CancellationToken);
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, response.StatusCode, $"request {i + 1} was limited");
        }

        var limited = await client.PostAsJsonAsync("/api/v1/identity/login", new { userName = "nobody", password = "wrong" }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
