using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class OrderIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private static object Order(Guid productId, IEnumerable<string> userIds, int amount = 1, bool split = false) =>
        new { products = new[] { productId }, users = userIds.Select(Guid.Parse).ToArray(), amount, split };

    [TestMethod]
    public async Task Create_AsUser_DeductsSaldoAndStock_AndAppearsInPersonal()
    {
        var product = await SeedProductAsync(price: 1.50m, stock: 10);
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);

        var response = await client.PostAsJsonAsync("/api/v1/order", Order(product.Id, [user.Id], amount: 2), CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        (await GetSaldoAsync(user.Id)).Should().Be(7.00m);
        (await GetStockAsync(product.Id)).Should().Be(8);

        var personal = await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/order/personal", CT));
        personal.GetProperty("totalCount").GetInt32().Should().Be(1);
        var order = personal.GetProperty("items")[0];
        order.GetProperty("productName").GetString().Should().Be(product.Name);
        order.GetProperty("paid").GetDecimal().Should().Be(3.00m);
        order.GetProperty("amount").GetInt32().Should().Be(2);
    }

    [TestMethod]
    public async Task Create_SplitAcrossTwoUsers_ChargesEachUserHalf()
    {
        var product = await SeedProductAsync(price: 3.00m, stock: 10);
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);
        var other = await CreateUserAsync(Roles.User, saldo: 10m);

        var response = await client.PostAsJsonAsync("/api/v1/order", Order(product.Id, [user.Id, other.Id], amount: 2, split: true), CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        (await GetSaldoAsync(user.Id)).Should().Be(7.00m);
        (await GetSaldoAsync(other.Id)).Should().Be(7.00m);
    }

    [TestMethod]
    public async Task Create_OnBehalfOfOtherUser_NotifiesAndPushesToThatUser()
    {
        var product = await SeedProductAsync();
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);
        var other = await CreateUserAsync(Roles.User, saldo: 10m);

        var response = await client.PostAsJsonAsync("/api/v1/order", Order(product.Id, [other.Id]), CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        Factory.Push.Sent.Should().Contain(p => p.UserId == other.Id);
        var notifications = await WithDbAsync(db => db.Notification.CountAsync(n => n.UserId == other.Id, CT));
        notifications.Should().Be(1);
    }

    [TestMethod]
    public async Task Create_AsKiosk_OrdersForUserWithoutOnBehalfNotification()
    {
        var product = await SeedProductAsync(price: 2.00m);
        var (kiosk, _) = await CreateAuthenticatedClientAsync(Roles.Kiosk);
        var user = await CreateUserAsync(Roles.User, saldo: 10m);

        var response = await kiosk.PostAsJsonAsync("/api/v1/order", Order(product.Id, [user.Id]), CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        (await GetSaldoAsync(user.Id)).Should().Be(8.00m);
        Factory.Push.Sent.Should().NotContain(p => p.UserId == user.Id);
    }

    [TestMethod]
    public async Task Create_AsGuest_Returns403()
    {
        var product = await SeedProductAsync();
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.Guest);

        var response = await client.PostAsJsonAsync("/api/v1/order", Order(product.Id, [user.Id]), CT);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Create_Anonymous_Returns401()
    {
        var product = await SeedProductAsync();

        var response = await AnonymousClient().PostAsJsonAsync("/api/v1/order", Order(product.Id, [Guid.NewGuid().ToString()]), CT);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Create_UnknownProduct_Returns404()
    {
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);

        var response = await client.PostAsJsonAsync("/api/v1/order", Order(Guid.NewGuid(), [user.Id]), CT);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Create_UnknownUser_Returns404()
    {
        var product = await SeedProductAsync();
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);

        var response = await client.PostAsJsonAsync("/api/v1/order", Order(product.Id, [Guid.NewGuid().ToString()]), CT);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1001)]
    public async Task Create_AmountOutOfRange_Returns400(int amount)
    {
        var product = await SeedProductAsync();
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);

        var response = await client.PostAsJsonAsync("/api/v1/order", Order(product.Id, [user.Id], amount), CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetSaldoAsync(user.Id)).Should().Be(10m);
    }

    [TestMethod]
    public async Task Create_NoProducts_Returns400()
    {
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);

        var response = await client.PostAsJsonAsync("/api/v1/order",
            new { products = Array.Empty<Guid>(), users = new[] { Guid.Parse(user.Id) }, amount = 1, split = false }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Initialize_ExcludesOutOfStockProducts()
    {
        var inStock = await SeedProductAsync(stock: 5);
        var soldOut = await SeedProductAsync(stock: 0);
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);

        var body = await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/order/initialize", CT));

        var productIds = body.GetProperty("products").EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToList();
        productIds.Should().Contain(inStock.Id).And.NotContain(soldOut.Id);
    }

    [TestMethod]
    public async Task Initialize_ListsUsersButNotKioskOrGuestAccounts()
    {
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);
        var kiosk = await CreateUserAsync(Roles.Kiosk);
        var guest = await CreateUserAsync(Roles.Guest);

        var body = await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/order/initialize", CT));

        var userIds = body.GetProperty("users").EnumerateArray().Select(u => u.GetProperty("id").GetString()).ToList();
        userIds.Should().Contain(user.Id).And.NotContain(kiosk.Id).And.NotContain(guest.Id);
    }

    [TestMethod]
    public async Task GetAll_AsUser_Returns403_AsAdmin_Returns200()
    {
        var (userClient, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (adminClient, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        (await userClient.GetAsync("/api/v1/order", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await adminClient.GetAsync("/api/v1/order", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task GetById_OwnerSucceeds_OtherUserIsForbidden_UnknownIsNotFound()
    {
        var product = await SeedProductAsync();
        var (owner, ownerUser) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);
        var (stranger, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var orderId = await PlaceOrderAsync(owner, product.Id, ownerUser.Id);

        (await owner.GetAsync($"/api/v1/order/{orderId}", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await stranger.GetAsync($"/api/v1/order/{orderId}", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await owner.GetAsync($"/api/v1/order/{Guid.NewGuid()}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Delete_ByOwner_RefundsSaldoAndRestocks()
    {
        var product = await SeedProductAsync(price: 2.00m, stock: 10);
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);
        var orderId = await PlaceOrderAsync(client, product.Id, user.Id);

        var response = await client.DeleteAsync($"/api/v1/order/{orderId}", CT);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await Describe(response));
        (await GetSaldoAsync(user.Id)).Should().Be(10m);
        (await GetStockAsync(product.Id)).Should().Be(10);
    }

    [TestMethod]
    public async Task Delete_ByOtherUser_Returns403_ByAdmin_Returns204()
    {
        var product = await SeedProductAsync();
        var (owner, ownerUser) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);
        var (stranger, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var orderId = await PlaceOrderAsync(owner, product.Id, ownerUser.Id);

        (await stranger.DeleteAsync($"/api/v1/order/{orderId}", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.DeleteAsync($"/api/v1/order/{orderId}", CT)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task Latest_IsAnonymous()
    {
        var response = await AnonymousClient().GetAsync("/api/v1/order/latest", CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task CsvExport_AsAdmin_CompletesAndDownloadsCsv()
    {
        var product = await SeedProductAsync(name: $"Csv {Guid.NewGuid():N}"[..12]);
        var (user, userEntity) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 10m);
        await PlaceOrderAsync(user, product.Id, userEntity.Id);
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        var start = await admin.PostAsync("/api/v1/order/export/csv", null, CT);
        start.StatusCode.Should().Be(HttpStatusCode.Accepted, await Describe(start));
        var jobId = (await ReadJsonAsync<JsonElement>(start)).GetProperty("id").GetGuid();

        var job = await PollAsync(
            async () => await ReadJsonAsync<JsonElement>(await admin.GetAsync($"/api/v1/order/export/csv/{jobId}", CT)),
            j => j.GetProperty("status").GetString() is "Completed" or "Failed");
        job.GetProperty("status").GetString().Should().Be("Completed", job.ToString());

        var download = await admin.GetAsync($"/api/v1/order/export/csv/{jobId}/download", CT);
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        (await download.Content.ReadAsStringAsync(CT)).Should().Contain(product.Name);
    }

    [TestMethod]
    public async Task CsvExport_UnknownJob_Returns404_AsUser_Returns403()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var (user, _) = await CreateAuthenticatedClientAsync(Roles.User);

        (await admin.GetAsync($"/api/v1/order/export/csv/{Guid.NewGuid()}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await user.PostAsync("/api/v1/order/export/csv", null, CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> PlaceOrderAsync(HttpClient client, Guid productId, string userId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/order", Order(productId, [userId]), CT);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        return await WithDbAsync(db => db.Order
            .Where(o => EF.Property<string>(o, "UserId") == userId)
            .OrderByDescending(o => o.CreatedOn)
            .Select(o => o.Id)
            .FirstAsync(CT));
    }
}
