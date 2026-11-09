using System.Net;
using System.Text.Json;
using FluentAssertions;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

/// <summary>
/// Sends requests through the real host, so controllers, MediatR handler registration and the
/// global exception handler are exercised together.
/// </summary>
[TestClass]
public class MediatRPipelineTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    [TestInitialize]
    public async Task SeedOnce()
    {
        if (!await WithDbAsync(db => Task.FromResult(db.Product.Any(p => p.Name == "Pipeline product"))))
            await SeedProductAsync(price: 1.25m, stock: 3, name: "Pipeline product");
    }

    [TestMethod]
    public async Task GetProducts_ReturnsPageFromHandler()
    {
        var client = AnonymousClient();

        var response = await client.GetAsync("/api/v1/product?skip=0&take=10", CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CT));
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items")[0].GetProperty("name").GetString().Should().Be("Pipeline product");
    }

    [TestMethod]
    public async Task GetProductById_UnknownId_Returns404()
    {
        var client = AnonymousClient();

        var response = await client.GetAsync($"/api/v1/product/{Guid.NewGuid()}", CT);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
