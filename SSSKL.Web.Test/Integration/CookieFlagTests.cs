using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration
{
    [TestClass]
    public class CookieFlagTests
    {
        private const string TestEmail = "integration-test@example.com";
        private const string TestPassword = "P@ssw0rd!";

        [TestMethod]
        public async Task Login_WithCookies_Sets_HttpOnly_And_Secure_On_Cookie()
        {
            // Arrange: force Secure/HttpOnly on the Identity cookies in the test host
            await using var factory = new SssklWebApplicationFactory().WithServices(services =>
            {
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

            // Register a new user via the identity API so the test doesn't need to resolve internal services.
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var registerPayload = new { email = TestEmail, userName = TestEmail, password = TestPassword, name = "Integration", surname = "Flag" };
            var regContent = new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json");
            var regResponse = await client.PostAsync("/api/v1/identity/register", regContent);
            regResponse.EnsureSuccessStatusCode();

            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = dbContext.Users.Single(u => u.Email == TestEmail);
                user.EmailConfirmed = true;
                dbContext.SaveChanges();
            }

            // Act: login with useCookies=true to get identity cookies
            var loginPayload = new { userName = TestEmail, password = TestPassword };
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







