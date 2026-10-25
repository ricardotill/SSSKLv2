using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SSSKLv2.Data;
using SSSKLv2.Data.Constants;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Services;

[TestClass]
public class TurnstileServiceTests : RepositoryTest
{
    [TestInitialize]
    public void TestInitialize() => InitializeDatabase();

    [TestCleanup]
    public void TestCleanup() => CleanupDatabase();

    private void Configure(string enabled = "true", string? siteKey = "site-key", string? secret = "secret-key", string? hostnames = null)
    {
        using var context = new ApplicationDbContext(GetOptions());
        void Set(string key, string? value)
        {
            if (value != null) context.GlobalSetting.Add(new GlobalSetting { Key = key, Value = value, UpdatedOn = DateTime.UtcNow });
        }
        Set(GlobalSettingsKeys.TurnstileEnabled, enabled);
        Set(GlobalSettingsKeys.TurnstileSiteKey, siteKey);
        Set(GlobalSettingsKeys.TurnstileSecretKey, secret);
        Set(GlobalSettingsKeys.TurnstileAllowedHostnames, hostnames);
        context.SaveChanges();
    }

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

    private TurnstileService Create(HttpMessageHandler handler) =>
        new(new HttpClient(handler),
            new MockDbContextFactory(GetOptions()),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new TurnstileOptions { AllowedHostnames = ["ssskl.scoutingwilo.nl"] }),
            NullLogger<TurnstileService>.Instance);

    [TestMethod]
    public async Task VerifyAsync_ValidToken_Passes_AndSendsSecretTokenAndIp()
    {
        Configure();
        var handler = Responding("""{"success":true,"action":"login","hostname":"ssskl.scoutingwilo.nl"}""");

        var result = await Create(handler).VerifyAsync("token-1", "login", "203.0.113.5");

        result.Success.Should().BeTrue();
        var body = handler.RequestBodies.Single();
        body.Should().Contain("secret=secret-key").And.Contain("response=token-1").And.Contain("remoteip=203.0.113.5").And.Contain("idempotency_key=");
    }

    [TestMethod]
    [DataRow("false", "site-key", "secret-key")]
    [DataRow("true", null, "secret-key")]
    [DataRow("true", "site-key", null)]
    public async Task VerifyAsync_WhenDisabledOrIncomplete_PassesWithoutCallingCloudflare(string enabled, string? siteKey, string? secret)
    {
        Configure(enabled, siteKey, secret);
        var handler = Responding("{}");
        var sut = Create(handler);

        (await sut.GetClientConfigAsync()).Should().Be(new TurnstileClientConfig(false, null));
        (await sut.VerifyAsync(null, "login", null)).Success.Should().BeTrue();
        handler.RequestBodies.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetClientConfigAsync_WhenEnabled_ReturnsSiteKeyButNeverTheSecret()
    {
        Configure();

        var config = await Create(Responding("{}")).GetClientConfigAsync();

        config.Should().Be(new TurnstileClientConfig(true, "site-key"));
    }

    [TestMethod]
    public async Task VerifyAsync_HostnamesFromGlobalSettings_OverrideDefaults()
    {
        Configure(hostnames: "kiosk.example.org, other.example.org");
        var handler = Responding("""{"success":true,"action":"login","hostname":"kiosk.example.org"}""");

        (await Create(handler).VerifyAsync("token", "login", null)).Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task VerifyAsync_SecretRejectedByCloudflare_LetsRequestThroughSoAdminsAreNotLockedOut()
    {
        Configure();
        var handler = Responding("""{"success":false,"error-codes":["invalid-input-secret"]}""");

        (await Create(handler).VerifyAsync("token", "login", null)).Success.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task VerifyAsync_MissingToken_FailsWithoutCallingCloudflare(string? token)
    {
        Configure();
        var handler = Responding("{}");

        var result = await Create(handler).VerifyAsync(token, "login", null);

        result.Success.Should().BeFalse();
        handler.RequestBodies.Should().BeEmpty();
    }

    [TestMethod]
    public async Task VerifyAsync_ReplayedToken_Fails()
    {
        Configure();
        var handler = Responding("""{"success":false,"error-codes":["timeout-or-duplicate"]}""");

        var result = await Create(handler).VerifyAsync("used-token", "login", null);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("timeout-or-duplicate");
    }

    [TestMethod]
    public async Task VerifyAsync_ActionMismatch_Fails()
    {
        Configure();
        var handler = Responding("""{"success":true,"action":"register","hostname":"ssskl.scoutingwilo.nl"}""");

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.FailureReason.Should().Be("action-mismatch");
    }

    [TestMethod]
    public async Task VerifyAsync_HostnameNotAllowed_Fails()
    {
        Configure();
        var handler = Responding("""{"success":true,"action":"login","hostname":"localhost"}""");

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.FailureReason.Should().Be("hostname-mismatch");
    }

    [TestMethod]
    public async Task VerifyAsync_SiteverifyDown_FailsClosed()
    {
        Configure();
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("siteverify-unavailable");
    }

    [TestMethod]
    public async Task VerifyAsync_SiteverifyServerError_FailsClosed()
    {
        Configure();
        var handler = Responding("oops", HttpStatusCode.BadGateway);

        var result = await Create(handler).VerifyAsync("token", "login", null);

        result.FailureReason.Should().Be("siteverify-unavailable");
    }
}
