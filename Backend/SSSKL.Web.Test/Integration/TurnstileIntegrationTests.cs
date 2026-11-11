using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SSSKL.Contracts.Services;
using SSSKL.Web.Registrations;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class TurnstileIntegrationTests
{
    private static SssklWebApplicationFactory CreateFactory(ITurnstileService turnstile) =>
        new SssklWebApplicationFactory().WithServices(services =>
        {
            services.RemoveAll<ITurnstileService>();
            services.AddSingleton(turnstile);
        });

    private static ITurnstileService EnabledTurnstile()
    {
        var turnstile = Substitute.For<ITurnstileService>();
        turnstile.VerifyAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(TurnstileVerificationResult.Failed("missing-input-response"));
        turnstile.VerifyAsync("good-token", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(TurnstileVerificationResult.Passed);
        return turnstile;
    }

    [TestMethod]
    public async Task ForgotPassword_WithoutToken_IsRejectedBeforeHandler()
    {
        var turnstile = EnabledTurnstile();
        await using var factory = CreateFactory(turnstile);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email = "nobody@example.com" });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "turnstile_failed");
    }

    [TestMethod]
    public async Task ForgotPassword_WithValidToken_ReachesHandlerWithExpectedAction()
    {
        var turnstile = EnabledTurnstile();
        await using var factory = CreateFactory(turnstile);
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/forgotPassword")
        {
            Content = JsonContent.Create(new { email = "nobody@example.com" })
        };
        request.Headers.Add(TurnstileEndpointFilter.HeaderName, "good-token");
        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        await turnstile.Received(1).VerifyAsync("good-token", "forgot-password", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Login_WithoutToken_IsRejected()
    {
        await using var factory = CreateFactory(EnabledTurnstile());
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new { userName = "x", password = "y" });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Refresh_IsNotGated()
    {
        var turnstile = EnabledTurnstile();
        await using var factory = CreateFactory(turnstile);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/identity/refresh", new { refreshToken = "invalid" });

        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        await turnstile.DidNotReceiveWithAnyArgs().VerifyAsync(default, default!, default, default);
    }

    [TestMethod]
    public async Task PublicTurnstileConfig_IsAnonymousAndReturnsSiteKey()
    {
        var turnstile = EnabledTurnstile();
        turnstile.GetClientConfigAsync(Arg.Any<CancellationToken>()).Returns(new TurnstileClientConfig(true, "site-key"));
        await using var factory = CreateFactory(turnstile);
        var client = factory.CreateClient();

        var config = await client.GetFromJsonAsync<Dictionary<string, object>>("/api/v1/GlobalSettings/public/turnstile");

        Assert.AreEqual("True", config!["enabled"].ToString());
        Assert.AreEqual("site-key", config["siteKey"].ToString());
    }

    [TestMethod]
    public async Task TurnstileSecret_CannotBeReadBack()
    {
        await using var factory = CreateFactory(EnabledTurnstile());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/GlobalSettings/TurnstileSecretKey");

        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
