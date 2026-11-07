using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class NotificationIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private async Task<int> UnreadCountAsync(HttpClient client) =>
        await ReadJsonAsync<int>(await client.GetAsync("/api/v1/notifications/unread-count", CT));

    private async Task SendCustomAsync(HttpClient admin, string userId, string title = "Hallo")
    {
        var response = await admin.PostAsJsonAsync("/api/v1/notifications/custom",
            new { title, message = "Bericht", linkUri = "/pos", fanOut = false, sendPush = true, userIds = new[] { userId } }, CT);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
    }

    [TestMethod]
    public async Task Custom_AsAdmin_CreatesNotificationAndPush()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);

        await SendCustomAsync(admin, user.Id, "Welkom");

        (await UnreadCountAsync(client)).Should().Be(1);
        var list = await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/notifications", CT));
        list.EnumerateArray().Single().GetProperty("title").GetString().Should().Be("Welkom");
        Factory.Push.Sent.Should().Contain(p => p.UserId == user.Id && p.Title == "Welkom");
    }

    [TestMethod]
    public async Task Custom_AsUser_Returns403()
    {
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);

        var response = await client.PostAsJsonAsync("/api/v1/notifications/custom",
            new { title = "x", message = "y", userIds = new[] { user.Id } }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Custom_ExternalLink_Returns400()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var user = await CreateUserAsync(Roles.User);

        var response = await admin.PostAsJsonAsync("/api/v1/notifications/custom",
            new { title = "x", message = "y", linkUri = "https://evil.example", userIds = new[] { user.Id } }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task MarkAsRead_And_ReadAll_LowerUnreadCount()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);
        await SendCustomAsync(admin, user.Id, "Een");
        await SendCustomAsync(admin, user.Id, "Twee");
        await SendCustomAsync(admin, user.Id, "Drie");
        (await UnreadCountAsync(client)).Should().Be(3);

        var first = (await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/notifications", CT)))[0].GetProperty("id").GetGuid();
        (await client.PostAsync($"/api/v1/notifications/{first}/read", null, CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await UnreadCountAsync(client)).Should().Be(2);

        (await client.PostAsync("/api/v1/notifications/read-all", null, CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await UnreadCountAsync(client)).Should().Be(0);
    }

    [TestMethod]
    public async Task OtherUsersNotifications_AreNotVisible()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var other = await CreateUserAsync(Roles.User);
        await SendCustomAsync(admin, other.Id);

        (await UnreadCountAsync(client)).Should().Be(0);
        (await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/notifications", CT))).GetArrayLength().Should().Be(0);
    }

    [TestMethod]
    public async Task Subscribe_ThenUnsubscribe_StoresAndRemovesSubscription()
    {
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);
        var endpoint = $"https://push.example.com/{Guid.NewGuid():N}";

        var subscribe = await client.PostAsJsonAsync("/api/v1/notifications/subscribe", new { endpoint, p256dh = "key", auth = "auth" }, CT);
        subscribe.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(subscribe));
        (await WithDbAsync(db => db.PushSubscription.CountAsync(s => s.UserId == user.Id, CT))).Should().Be(1);

        var unsubscribe = await client.PostAsJsonAsync("/api/v1/notifications/unsubscribe", new { endpoint }, CT);
        unsubscribe.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(unsubscribe));
        (await WithDbAsync(db => db.PushSubscription.CountAsync(s => s.UserId == user.Id, CT))).Should().Be(0);
    }

    [TestMethod]
    public async Task Anonymous_Returns401()
    {
        (await AnonymousClient().GetAsync("/api/v1/notifications", CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
