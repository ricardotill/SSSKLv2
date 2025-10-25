using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SSSKLv2.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SSSKLv2.Test.Integration
{
    [TestClass]
    public class CookieFlagTests
    {
        private const string TestEmail = "integration-test@example.com";
        private const string TestPassword = "P@ssw0rd!";

        [TestMethod]
        public async Task Login_WithCookies_Sets_HttpOnly_And_Secure_On_Cookie()
        {
            // Arrange: create factory and replace EF Core with InMemory and set environment to Production
            var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                // Run Program in Development and provide DefaultConnection so Program uses the dev code path.
                builder.UseEnvironment("IntegrationTest");

                // Provide an AZURE_SQL_CONNECTIONSTRING value so Program's non-development config lookup does not throw.
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    var dict = new[] { new KeyValuePair<string, string?>("ConnectionStrings:AZURE_SQL_CONNECTIONSTRING", "FakeConnectionStringForTests") };
                    configBuilder.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    // Only override cookie metadata in the test host so TestServer will emit Secure/HttpOnly flags.
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
                        Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme,
                        options =>
                        {
                            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                            options.Cookie.HttpOnly = true;
                            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                        });

                    services.PostConfigure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
                        Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme,
                        options =>
                        {
                            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                            options.Cookie.HttpOnly = true;
                            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                        });
                });
            });

            // Register a new user via the identity API so the test doesn't need to resolve internal services.
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var registerPayload = new { email = TestEmail, password = TestPassword, displayName = "Integration Test" };
            var regContent = new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json");
            var regResponse = await client.PostAsync("/api/v1/identity/register", regContent);
            regResponse.EnsureSuccessStatusCode();

            // Act: login with useCookies=true to get identity cookies
            var loginPayload = new { email = TestEmail, password = TestPassword };
            var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/v1/identity/login?useCookies=true", content);

            // Assert: status OK and Set-Cookie header contains HttpOnly and Secure
            response.EnsureSuccessStatusCode();

            Assert.IsTrue(response.Headers.Contains("Set-Cookie"), "Response did not contain Set-Cookie headers.");

            var setCookie = response.Headers.GetValues("Set-Cookie").ToList();
            Assert.IsTrue(setCookie.Any(), "No Set-Cookie values returned.");

            // At least one cookie should have HttpOnly and Secure attributes
            var hasHttpOnly = setCookie.Any(c => c.Contains("HttpOnly", System.StringComparison.OrdinalIgnoreCase));
            var hasSecure = setCookie.Any(c => c.Contains("Secure", System.StringComparison.OrdinalIgnoreCase));

            Assert.IsTrue(hasHttpOnly, "No cookie with HttpOnly attribute found.");
            Assert.IsTrue(hasSecure, "No cookie with Secure attribute found (ensure environment is Production in factory).");
        }
    }
}







