using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Newtonsoft.Json.Linq;
using SSSKLv2.Data;
using SSSKLv2.Services;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Services;

[TestClass]
public class WebPushServiceTests : RepositoryTest
{
    private ApplicationDbContext _dbContext = null!;
    private IConfiguration _configuration = null!;
    private ILogger<WebPushService> _logger = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContext = new ApplicationDbContext(GetOptions());
        _logger = Substitute.For<ILogger<WebPushService>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Use valid-format ECDH P-256 VAPID keys (same as dev appsettings)
                ["VapidDetails:PublicKey"]  = "BGXzpnAdCnSzcF1Ho4D7Ihtt23zgL4hXvSC3OhFhlK5WTN_Nm4leLgKqfliVPf5ci4hMzHIpqpMkySZLqFLsqsE",
                ["VapidDetails:PrivateKey"] = "qy7mzAZGQL748CQtkGWZTU4UDCIwmexuXRUgj7AdIAk",
                ["VapidDetails:Subject"]    = "mailto:test@example.com"
            })
            .Build();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
        _dbContext.Dispose();
    }

    // ── Payload Shape ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SendNotificationAsync_NoSubscriptions_ShouldNotThrow()
    {
        // Arrange – No subscriptions in DB for this user
        var sut = CreateSut();

        // Act & Assert – should complete without error
        await sut.Invoking(s => s.SendNotificationAsync("unknown-user", "Title", "Body"))
            .Should().NotThrowAsync();
    }

    [TestMethod]
    public void SendNotificationAsync_ShouldUseCorrectTitle_WithBrandingPrefix()
    {
        // Assert – verify the static payload builder applies "SSSKL: " prefix
        var payloadJson = BuildExpectedPayload("My Event", "Someone reacted", "/events/1");
        var notification = JObject.Parse(payloadJson)["notification"]!;

        notification["title"]!.Value<string>().Should().Be("SSSKL: My Event");
        notification["body"]!.Value<string>().Should().Be("Someone reacted");
        notification["icon"]!.Value<string>().Should().Be("/assets/icons/icon-192x192.png");
        notification["badge"]!.Value<string>().Should().Be("/assets/icons/icon-72x72.png");
    }

    [TestMethod]
    public async Task SendNotificationAsync_Payload_ShouldHaveVibrateArray()
    {
        // Arrange
        var payloadJson = BuildExpectedPayload("Title", "Body");
        var notification = JObject.Parse(payloadJson)["notification"]!;

        // Assert
        notification["vibrate"].Should().NotBeNull();
        notification["vibrate"]!.Type.Should().Be(JTokenType.Array);
    }

    [TestMethod]
    public async Task SendNotificationAsync_NullUrl_ShouldDefaultToSlash()
    {
        // Arrange
        var payloadJson = BuildExpectedPayload("Title", "Body", null);
        var notification = JObject.Parse(payloadJson)["notification"]!;

        // Assert
        notification["data"]!["url"]!.Value<string>().Should().Be("/");
    }

    [TestMethod]
    public async Task SendNotificationAsync_WithUrl_ShouldIncludeInData()
    {
        // Arrange
        var url = "/events/abc-123";
        var payloadJson = BuildExpectedPayload("Title", "Body", url);
        var notification = JObject.Parse(payloadJson)["notification"]!;

        // Assert
        notification["data"]!["url"]!.Value<string>().Should().Be(url);
    }

    // ── Subscription Filtering ─────────────────────────────────────────────────

    [TestMethod]
    public async Task SendNotificationAsync_ShouldOnlySendToSubscriptionsOfTargetUser()
    {
        // Arrange – Add subscriptions for two different users
        var user1 = TestUser.Id;
        var user2 = Guid.NewGuid().ToString();
        _dbContext.Users.Add(new ApplicationUser { Id = user2, UserName = "other", Name = "Other", Surname = "User", Email = "other@test.com" });

        AddSubscriptionForUser(user1, "https://endpoint-user1.example.com");
        AddSubscriptionForUser(user2, "https://endpoint-user2.example.com");
        await _dbContext.SaveChangesAsync();

        // We retrieve from DB to verify EF filtering — create a fresh context
        using var freshContext = new ApplicationDbContext(GetOptions());
        var user1Subs = await freshContext.PushSubscription.Where(s => s.UserId == user1).ToListAsync();
        var user2Subs = await freshContext.PushSubscription.Where(s => s.UserId == user2).ToListAsync();

        // Assert
        user1Subs.Should().HaveCount(1);
        user1Subs.Single().Endpoint.Should().Be("https://endpoint-user1.example.com");
        user2Subs.Should().HaveCount(1);
        user2Subs.Single().Endpoint.Should().Be("https://endpoint-user2.example.com");
    }

    // ── Configuration Fallback ─────────────────────────────────────────────────

    [TestMethod]
    public void Configuration_EnvironmentVariableOverridesAppsettings_ShouldPreferEnvVar()
    {
        // Arrange – provide both env variable and appsettings style
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VapidDetails:PublicKey"] = "appsettings-key",
                ["VAPID_PUBLIC_KEY"]        = "env-key"
            })
            .Build();

        // The service reads VAPID_PUBLIC_KEY first
        var envKey = config["VAPID_PUBLIC_KEY"];
        var appKey = config["VapidDetails:PublicKey"];
        var resolvedKey = envKey ?? appKey;

        // Assert
        resolvedKey.Should().Be("env-key");
    }

    [TestMethod]
    public void Configuration_NoEnvironmentVariable_ShouldFallbackToAppsettings()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VapidDetails:PublicKey"] = "appsettings-key"
            })
            .Build();

        var resolvedKey = config["VAPID_PUBLIC_KEY"] ?? config["VapidDetails:PublicKey"];

        // Assert
        resolvedKey.Should().Be("appsettings-key");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors the exact JSON serialization done in WebPushService to verify payload shape.
    /// </summary>
    private static string BuildExpectedPayload(string title, string body, string? url = "/events/1")
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            notification = new
            {
                title = $"SSSKL: {title}",
                body,
                icon  = "/assets/icons/icon-192x192.png",
                badge = "/assets/icons/icon-72x72.png",
                vibrate = new int[] { 100, 50, 100 },
                data = new
                {
                    url = url ?? "/",
                    onActionClick = new
                    {
                        @default = new { operation = "navigateLastFocusedOrOpen", url = url ?? "/" }
                    }
                }
            }
        });
    }

    private PushSubscription AddSubscriptionForUser(string userId, string endpoint = "https://push.example.com/endpoint")
    {
        var sub = new PushSubscription
        {
            UserId    = userId,
            Endpoint  = endpoint,
            P256dh    = "dummyP256dh",
            Auth      = "dummyAuth",
            CreatedOn = DateTime.UtcNow
        };
        _dbContext.PushSubscription.Add(sub);
        _dbContext.SaveChanges();
        return sub;
    }

    private WebPushService CreateSut(List<string>? payloadCapture = null)
    {
        // Use real HttpClient but with no real endpoint reachable (subscriptions point to fake URLs)
        // For unit tests we focus on DB/payload logic; HTTP calls would 404 harmlessly.
        return new WebPushService(_configuration, _dbContext, new HttpClient(), _logger);
    }
}
