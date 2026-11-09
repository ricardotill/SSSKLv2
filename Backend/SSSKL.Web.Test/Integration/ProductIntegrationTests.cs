using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class ProductIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private static string UniqueName() => $"Product {Guid.NewGuid():N}"[..20];

    private static object NewProduct(string name, decimal price = 1.25m, int stock = 5) =>
        new { name, description = "desc", price, stock, enableLeaderboard = true };

    [TestMethod]
    public async Task GetAll_Anonymous_ReturnsPagedAndFullList()
    {
        var product = await SeedProductAsync();
        var client = AnonymousClient();

        var paged = await client.GetAsync("/api/v1/product?skip=0&take=100", CT);
        paged.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync<JsonElement>(paged)).GetProperty("items").EnumerateArray()
            .Should().Contain(p => p.GetProperty("id").GetGuid() == product.Id);

        var all = await ReadJsonAsync<JsonElement>(await client.GetAsync("/api/v1/product?all=true", CT));
        all.ValueKind.Should().Be(JsonValueKind.Array);
        all.EnumerateArray().Should().Contain(p => p.GetProperty("id").GetGuid() == product.Id);
    }

    [TestMethod]
    public async Task Create_AsAdmin_Returns201_ThenGetByIdReturnsIt()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var name = UniqueName();

        var response = await admin.PostAsJsonAsync("/api/v1/product", NewProduct(name, 2.75m, 12), CT);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await Describe(response));
        var fetched = await ReadJsonAsync<JsonElement>(await AnonymousClient().GetAsync(response.Headers.Location, CT));
        fetched.GetProperty("name").GetString().Should().Be(name);
        fetched.GetProperty("price").GetDecimal().Should().Be(2.75m);
        fetched.GetProperty("stock").GetInt32().Should().Be(12);
    }

    [TestMethod]
    public async Task Create_MissingPrice_Returns400()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        var response = await admin.PostAsJsonAsync("/api/v1/product", new { name = UniqueName(), stock = 1, enableLeaderboard = true }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Create_DuplicateName_Returns409()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var existing = await SeedProductAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/product", NewProduct(existing.Name), CT);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await Describe(response));
    }

    [TestMethod]
    public async Task Update_ToOtherProductsName_Returns409()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var first = await SeedProductAsync();
        var second = await SeedProductAsync();

        var response = await admin.PutAsJsonAsync($"/api/v1/product/{second.Id}",
            new { id = second.Id, name = first.Name, price = 1m, stock = 1, enableLeaderboard = true }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await Describe(response));
    }

    [TestMethod]
    public async Task Create_AsUser_Returns403_Anonymous_Returns401()
    {
        var (user, _) = await CreateAuthenticatedClientAsync(Roles.User);

        (await user.PostAsJsonAsync("/api/v1/product", NewProduct(UniqueName()), CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await AnonymousClient().PostAsJsonAsync("/api/v1/product", NewProduct(UniqueName()), CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Update_AsAdmin_PersistsChanges()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var product = await SeedProductAsync(price: 1m, stock: 1);
        var newName = UniqueName();

        var response = await admin.PutAsJsonAsync($"/api/v1/product/{product.Id}",
            new { id = product.Id, name = newName, price = 3.5m, stock = 9, enableLeaderboard = false }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await Describe(response));
        var fetched = await ReadJsonAsync<JsonElement>(await AnonymousClient().GetAsync($"/api/v1/product/{product.Id}", CT));
        fetched.GetProperty("name").GetString().Should().Be(newName);
        fetched.GetProperty("price").GetDecimal().Should().Be(3.5m);
        fetched.GetProperty("stock").GetInt32().Should().Be(9);
        fetched.GetProperty("enableLeaderboard").GetBoolean().Should().BeFalse();
    }

    [TestMethod]
    public async Task Update_Unknown_Returns404()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var id = Guid.NewGuid();

        var response = await admin.PutAsJsonAsync($"/api/v1/product/{id}",
            new { id, name = UniqueName(), price = 1m, stock = 1, enableLeaderboard = true }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Delete_AsAdmin_Returns204_ThenGetReturns404()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var product = await SeedProductAsync();

        (await admin.DeleteAsync($"/api/v1/product/{product.Id}", CT)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await AnonymousClient().GetAsync($"/api/v1/product/{product.Id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.DeleteAsync($"/api/v1/product/{product.Id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
