using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class LeaderboardIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private async Task OrderAsync(HttpClient client, Guid productId, string userId, int amount)
    {
        var response = await client.PostAsJsonAsync("/api/v1/order",
            new { products = new[] { productId }, users = new[] { Guid.Parse(userId) }, amount, split = false }, CT);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
    }

    private async Task<List<JsonElement>> GetAsync(string url)
    {
        var response = await AnonymousClient().GetAsync(url, CT);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        return (await ReadJsonAsync<JsonElement>(response)).EnumerateArray().ToList();
    }

    [TestMethod]
    [DataRow("/api/v1/leaderboard/{0}")]
    [DataRow("/api/v1/leaderboard/monthly/{0}")]
    [DataRow("/api/v1/leaderboard/12hour/{0}")]
    public async Task Leaderboard_IsAnonymous_AndOrderedByAmount(string urlFormat)
    {
        var product = await SeedProductAsync(stock: 100);
        var (client, small) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 100m);
        var big = await CreateUserAsync(Roles.User, saldo: 100m);
        await OrderAsync(client, product.Id, small.Id, 1);
        await OrderAsync(client, product.Id, big.Id, 5);

        var entries = await GetAsync(string.Format(urlFormat, product.Id));

        entries.Select(e => e.GetProperty("userId").GetString()).Should().Equal(big.Id, small.Id);
        entries.Select(e => e.GetProperty("amount").GetInt32()).Should().Equal(5, 1);
        entries[0].GetProperty("position").GetInt32().Should().Be(1);
    }

    [TestMethod]
    public async Task Monthly_ExcludesOrdersOlderThanAMonth()
    {
        var product = await SeedProductAsync(stock: 100);
        var (client, recent) = await CreateAuthenticatedClientAsync(Roles.User, saldo: 100m);
        var old = await CreateUserAsync(Roles.User, saldo: 100m);
        await OrderAsync(client, product.Id, recent.Id, 1);
        await WithDbAsync(async db =>
        {
            db.Order.Add(new Order
            {
                User = await db.Users.FindAsync([old.Id], CT) ?? throw new InvalidOperationException(),
                Product = await db.Product.FindAsync([product.Id], CT),
                ProductNaam = product.Name,
                Amount = 10,
                Paid = 15m,
                CreatedOn = DateTime.Now.AddDays(-40),
            });
            return await db.SaveChangesAsync(CT);
        });

        var entries = await GetAsync($"/api/v1/leaderboard/monthly/{product.Id}");

        entries.Select(e => e.GetProperty("userId").GetString()).Should().Equal(recent.Id);
    }

    [TestMethod]
    public async Task UnknownProduct_Returns404()
    {
        var response = await AnonymousClient().GetAsync($"/api/v1/leaderboard/{Guid.NewGuid()}", CT);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
