using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SSSKLv2.Data;

namespace SSSKLv2.Test.Integration
{
    [TestClass]
    public class CookieRefreshTests
    {
        private const string TestEmail = "integration-refresh@example.com";
        private const string TestPassword = "P@ssw0rd!";

        [TestMethod]
        public async Task Login_WithCookies_Then_Refresh_Uses_HttpOnly_Cookies()
        {
            var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTest");

                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    var dict = new[] { new KeyValuePair<string, string?>("ConnectionStrings:AZURE_SQL_CONNECTIONSTRING", "FakeConnectionStringForTests") };
                    configBuilder.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    // Ensure cookies are always marked Secure/HttpOnly for the test host
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

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Register user
            var registerPayload = new { email = TestEmail, password = TestPassword, displayName = "Integration Refresh" };
            var regContent = new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json");
            var regResponse = await client.PostAsync("/api/v1/identity/register", regContent);
            var regBody = await regResponse.Content.ReadAsStringAsync();
            Console.WriteLine("[Test] Register response status: " + (int)regResponse.StatusCode + " body: " + regBody);
            if (!regResponse.IsSuccessStatusCode)
            {
                Assert.Fail($"Register failed: {(int)regResponse.StatusCode} {regResponse.ReasonPhrase}. Body: {regBody}");
            }

            // Login using cookies
            var loginPayload = new { email = TestEmail, password = TestPassword };
            var loginContent = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");
            var loginResponse = await client.PostAsync("/api/v1/identity/login?useCookies=true", loginContent);
            var loginBody = await loginResponse.Content.ReadAsStringAsync();
            Console.WriteLine("[Test] Login response status: " + (int)loginResponse.StatusCode + " body: " + loginBody);
            if (!loginResponse.IsSuccessStatusCode)
            {
                Assert.Fail($"Login failed: {(int)loginResponse.StatusCode} {loginResponse.ReasonPhrase}. Body: {loginBody}");
            }

            Assert.IsTrue(loginResponse.Headers.Contains("Set-Cookie"), "Login response did not contain Set-Cookie headers.");
            var setCookie = loginResponse.Headers.GetValues("Set-Cookie").ToList();
            Assert.IsTrue(setCookie.Any(), "No Set-Cookie values returned from login.");

            var hasHttpOnly = setCookie.Any(c => c.Contains("HttpOnly", System.StringComparison.OrdinalIgnoreCase));
            var hasSecure = setCookie.Any(c => c.Contains("Secure", System.StringComparison.OrdinalIgnoreCase));

            Assert.IsTrue(hasHttpOnly, "No cookie with HttpOnly attribute found after login.");
            Assert.IsTrue(hasSecure, "No cookie with Secure attribute found after login.");

            // Call refresh endpoint using the same client so cookies are sent automatically
            var refreshContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var refreshResponse = await client.PostAsync("/api/v1/identity/refresh?useCookies=true", refreshContent);
            var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
            Console.WriteLine("[Test] Refresh response status: " + (int)refreshResponse.StatusCode + " body: " + refreshBody);
            if (!refreshResponse.IsSuccessStatusCode)
            {
                Assert.Fail($"Refresh failed: {(int)refreshResponse.StatusCode} {refreshResponse.ReasonPhrase}. Body: {refreshBody}");
            }

            var json = await refreshResponse.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), "Refresh response body was empty.");

            using var doc = JsonDocument.Parse(json);
            // Expect the refresh to return an accessToken in JSON when using cookies
            Assert.IsTrue(doc.RootElement.TryGetProperty("accessToken", out var tokenProp) && !string.IsNullOrWhiteSpace(tokenProp.GetString()), "Refresh response did not contain an accessToken.");

            // Optionally, the refresh call may also set cookies again (rotated refresh token). Ensure at least one Set-Cookie or that cookies are still present in the handler's cookie container.
            // Check headers for Set-Cookie (may or may not be present depending on implementation)
            if (refreshResponse.Headers.Contains("Set-Cookie"))
            {
                var refreshSet = refreshResponse.Headers.GetValues("Set-Cookie").ToList();
                Assert.IsTrue(refreshSet.Any(), "Refresh returned empty Set-Cookie headers.");
            }
        }
    }
}
