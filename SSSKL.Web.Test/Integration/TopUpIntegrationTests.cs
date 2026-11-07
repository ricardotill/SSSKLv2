using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class TopUpIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    [TestMethod]
    public async Task Create_AsAdmin_Returns201WithLocation_AndIncreasesSaldo()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var user = await CreateUserAsync(Roles.User, saldo: 2m);

        var response = await admin.PostAsJsonAsync("/api/v1/topup", new { userName = user.UserName, saldo = 5.25m }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await Describe(response));
        response.Headers.Location.Should().NotBeNull();
        (await GetSaldoAsync(user.Id)).Should().Be(7.25m);

        var location = await admin.GetAsync(response.Headers.Location, CT);
        location.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync<JsonElement>(location)).GetProperty("saldo").GetDecimal().Should().Be(5.25m);
    }

    [TestMethod]
    public async Task Delete_AsAdmin_RevertsSaldo()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var user = await CreateUserAsync(Roles.User, saldo: 1m);
        var created = await admin.PostAsJsonAsync("/api/v1/topup", new { userName = user.UserName, saldo = 4m }, CT);
        var id = (await ReadJsonAsync<JsonElement>(created)).GetProperty("id").GetGuid();

        var response = await admin.DeleteAsync($"/api/v1/topup/{id}", CT);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await Describe(response));
        (await GetSaldoAsync(user.Id)).Should().Be(1m);
    }

    [TestMethod]
    public async Task Create_ZeroSaldo_Returns400()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var user = await CreateUserAsync(Roles.User);

        var response = await admin.PostAsJsonAsync("/api/v1/topup", new { userName = user.UserName, saldo = 0m }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Create_UnknownUserName_Returns404()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        var response = await admin.PostAsJsonAsync("/api/v1/topup", new { userName = "does-not-exist", saldo = 5m }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Create_AsUser_Returns403_Anonymous_Returns401()
    {
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);
        var body = new { userName = user.UserName, saldo = 5m };

        (await client.PostAsJsonAsync("/api/v1/topup", body, CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await AnonymousClient().PostAsJsonAsync("/api/v1/topup", body, CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetSaldoAsync(user.Id)).Should().Be(0m);
    }

    [TestMethod]
    public async Task GetById_Unknown_Returns404()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        (await admin.GetAsync($"/api/v1/topup/{Guid.NewGuid()}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Personal_ReturnsOnlyOwnTopUps_GetAllIsAdminOnly()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);
        var other = await CreateUserAsync(Roles.User);
        await admin.PostAsJsonAsync("/api/v1/topup", new { userName = user.UserName, saldo = 3m }, CT);
        await admin.PostAsJsonAsync("/api/v1/topup", new { userName = other.UserName, saldo = 7m }, CT);

        var personal = await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/topup/personal", CT));

        personal.GetProperty("totalCount").GetInt32().Should().Be(1);
        personal.GetProperty("items")[0].GetProperty("saldo").GetDecimal().Should().Be(3m);
        (await client.GetAsync("/api/v1/topup", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.GetAsync("/api/v1/topup", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
