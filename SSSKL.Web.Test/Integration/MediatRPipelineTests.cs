using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Web.Test.Integration;

/// <summary>
/// Sends requests through the real host, so controllers, MediatR handler registration and the
/// global exception handler are exercised together.
/// </summary>
[TestClass]
public class MediatRPipelineTests
{
    private static WebApplicationFactory<Program> _factory = default!;
    private static SqliteConnection _connection = default!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("IntegrationTest");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:db"] = "Filename=:memory:",
                    ["WEBSITE_DOMAIN"] = "https://localhost"
                });
            });
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                var factoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
                if (factoryDescriptor != null) services.Remove(factoryDescriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(_connection));
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        db.Product.Add(new Product { Id = Guid.NewGuid(), Name = "Pipeline product", Price = 1.25m, Stock = 3 });
        db.SaveChanges();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _factory?.Dispose();
        _connection?.Close();
    }

    [TestMethod]
    public async Task GetProducts_ReturnsPageFromHandler()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/product?skip=0&take=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items")[0].GetProperty("name").GetString().Should().Be("Pipeline product");
    }

    [TestMethod]
    public async Task GetProductById_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/product/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
