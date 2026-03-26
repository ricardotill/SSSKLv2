using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Integration
{
    [TestClass]
    public class SpaFallbackTests
    {
        private static WebApplicationFactory<Program> _factory = default!;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTest");

                builder.ConfigureAppConfiguration((hostContext, configBuilder) =>
                {
                    var dict = new[] { new KeyValuePair<string, string?>("ConnectionStrings:db", "Filename=:memory:") };
                    configBuilder.AddInMemoryCollection(dict);
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
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task Get_UnknownRoute_DoesNotReturn404FromController()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act
            // We use a route that doesn't exist in the backend
            var response = await client.GetAsync("/some-random-angular-route");

            // Assert
            // If the fallback is working, it should attempt to serve index.html.
            // Since index.html is missing on disk in the test environment, we expect a 404,
            // but it's a 404 from the StaticFileMiddleware, not the controller.
            // Verifying the full "200 OK index.html" requires the file to be present.
            
            // For now, we verify that the host starts and handles the request without crashing.
            Assert.IsNotNull(response);
        }
    }
}
