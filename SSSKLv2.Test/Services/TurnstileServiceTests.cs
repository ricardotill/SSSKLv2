using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SSSKLv2.Services;

namespace SSSKLv2.Test.Services;

[TestClass]
public class TurnstileServiceTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return await respond(request);
        }
    }

    private static StubHandler Responding(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

    private static TurnstileService Create(HttpMessageHandler handler, string? secret = "secret-key") =>
        new(new HttpClient(handler),
            Options.Create(new TurnstileOptions { SecretKey = secret, AllowedHostnames = ["ssskl.scoutingwilo.nl"] }),
            NullLogger<TurnstileService>.Instance);

    [TestMethod]
    public async Task VerifyAsync_ValidToken_Passes_AndSendsSecretTokenAndIp()
    {
        var handler = Responding("""{"success":true,"action":"login","hostname":"ssskl.scoutingwilo.nl"}""");

        var result = await Create(handler).VerifyAsync("token-1", "login", "203.0.113.5");

        result.Success.Should().BeTrue();
        var body = handler.RequestBodies.Single();
        body.Should().Contain("secret=secret-key").And.Contain("response=token-1").And.Contain("remoteip=203.0.113.5").And.Contain("idempotency_key=");
    }

    [TestMethod]
    public async Task VerifyAsync_WhenDisabled_PassesWithoutCallingCloudflare()
    {
        var handler = Responding("{}");
        var sut = Create(handler, secret: "");

        sut.IsEnabled.Should().BeFalse();
        (await sut.VerifyAsync(null, "login", null)).Success.Should().BeTrue();
        handler.RequestBodies.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task VerifyAsync_MissingToken_FailsWithoutCallingCloudflare(string? token)
    {
        var handler = Responding("{}");

        var result = await Create(handler).VerifyAsync(token, "login", null);

        result.Success.Should().BeFalse();
        handler.RequestBodies.Should().BeEmpty();
    }

    [TestMethod]
    public async Task VerifyAsync_ReplayedToken_Fails()
    {
        var handler = Responding("""{"success":false,"error-codes":["timeout-or-duplicate"]}""");

        var result = await Create(handler).VerifyAsync("used-token", "login", null);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("timeout-or-duplicate");
    }

    [TestMethod]
    public async Task VerifyAsync_ActionMismatch_Fails()
    {
        var handler = Responding("""{"success":true,"action":"register","hostname":"ssskl.scoutingwilo.nl"}""");

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.FailureReason.Should().Be("action-mismatch");
    }

    [TestMethod]
    public async Task VerifyAsync_HostnameNotAllowed_Fails()
    {
        var handler = Responding("""{"success":true,"action":"login","hostname":"localhost"}""");

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.FailureReason.Should().Be("hostname-mismatch");
    }

    [TestMethod]
    public async Task VerifyAsync_SiteverifyDown_FailsClosed()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("siteverify-unavailable");
    }

    [TestMethod]
    public async Task VerifyAsync_SiteverifyServerError_FailsClosed()
    {
        var handler = Responding("oops", HttpStatusCode.BadGateway);

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.FailureReason.Should().Be("siteverify-unavailable");
    }
}
