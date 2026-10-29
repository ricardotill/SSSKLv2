using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class RateLimitingTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("IntegrationTest");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:db", "Filename=:memory:")]);
            });
            builder.ConfigureServices(services =>
            {
                var connection = new SqliteConnection("Filename=:memory:");
                connection.Open();

                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                var factoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
                if (factoryDescriptor != null) services.Remove(factoryDescriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
                services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(connection));
            });
        });

    [TestMethod]
    public async Task ForgotPassword_AfterLimit_Returns429WithRetryAfter()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email = "nobody@example.com" });
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode, $"request {i + 1} was limited");
        }

        var limited = await client.PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email = "nobody@example.com" });

        Assert.AreEqual(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.IsTrue(limited.Headers.Contains("Retry-After"));
    }

    [TestMethod]
    public async Task UnlistedEndpoints_AreNotLimited()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 40; i++)
        {
            var response = await client.GetAsync("/healthz");
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
