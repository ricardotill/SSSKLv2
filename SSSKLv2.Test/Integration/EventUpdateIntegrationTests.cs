using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;

namespace SSSKLv2.Test.Integration
{
    [TestClass]
    public class EventUpdateIntegrationTests
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

                builder.ConfigureAppConfiguration((hostContext, configBuilder) =>
                {
                    var dict = new[] { 
                        new KeyValuePair<string, string?>("ConnectionStrings:db", "Filename=:memory:"),
                        new KeyValuePair<string, string?>("WEBSITE_DOMAIN", "https://localhost")
                    };
                    configBuilder.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    
                    var factoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
                    if (factoryDescriptor != null) services.Remove(factoryDescriptor);

                    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                    services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(_connection));

                    // Mock IBlobStorageAgent to avoid external Azure calls in integration tests
                    var mockBlobAgent = NSubstitute.Substitute.For<SSSKLv2.Agents.IBlobStorageAgent>();
                    mockBlobAgent.UploadFileToBlobAsync(NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<Stream>())
                        .Returns(Task.FromResult<BlobStorageItem>(new EventImage
                        {
                            Id = Guid.NewGuid(),
                            FileName = "test.png",
                            Uri = "/fake/test.png",
                            ContentType = "image/png",
                            CreatedOn = DateTime.UtcNow
                        }));

                    var blobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(SSSKLv2.Agents.IBlobStorageAgent));
                    if (blobDescriptor != null) services.Remove(blobDescriptor);
                    services.AddSingleton(mockBlobAgent);
                });
            });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _factory?.Dispose();
            _connection?.Close();
        }

        private async Task<HttpClient> CreateAuthenticatedClientAsync(string emailPrefix)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var email = $"{emailPrefix}@example.com";
            var password = "P@ssw0rd123!";

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser { UserName = email, Email = email, Name = "Event", Surname = "Tester", EmailConfirmed = true };
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, "User");
            }

            var loginPayload = new { userName = email, password };
            var loginResponse = await client.PostAsync("/api/v1/identity/login", new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json"));
            loginResponse.IsSuccessStatusCode.Should().BeTrue();

            var loginBody = await loginResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(loginBody);
            var token = doc.RootElement.GetProperty("accessToken").GetString();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [TestMethod]
        public async Task UpdateEvent_WithHttpPostMultipartFormData_UpdatesEventCorrectly()
        {
            var client = await CreateAuthenticatedClientAsync("event-post-test");

            Guid eventId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = db.Users.First(u => u.Email == "event-post-test@example.com");

                var initialEvent = new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Original Title",
                    Description = "Original Description",
                    StartDateTime = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
                    EndDateTime = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
                    CreatorId = user.Id,
                    CreatedOn = DateTime.UtcNow
                };
                db.Event.Add(initialEvent);
                await db.SaveChangesAsync();
                eventId = initialEvent.Id;
            }

            // Send text-only update (no image) to avoid SQLite GETUTCDATE() incompatibility
            // in HasDefaultValueSql used by BlobStorageItem.CreatedOn
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("POST Updated Title"), "Title");
            content.Add(new StringContent("POST Updated Description"), "Description");
            content.Add(new StringContent(new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ")), "StartDateTime");
            content.Add(new StringContent(new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ")), "EndDateTime");

            var response = await client.PostAsync($"/api/v1/Events/{eventId}", content);
            var resBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Post Status: {response.StatusCode}, Body: {resBody}");
            response.StatusCode.Should().Be(HttpStatusCode.NoContent, because: resBody);

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedEvent = db2.Event.FirstOrDefault(e => e.Id == eventId);

            updatedEvent.Should().NotBeNull();
            updatedEvent!.Title.Should().Be("POST Updated Title");
            updatedEvent.Description.Should().Be("POST Updated Description");
        }

        [TestMethod]
        public async Task UpdateEvent_WithEmptyForm_ReturnsBadRequest()
        {
            var client = await CreateAuthenticatedClientAsync("event-bad-test");

            Guid eventId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = db.Users.First(u => u.Email == "event-bad-test@example.com");

                var initialEvent = new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Keep Me Intact",
                    Description = "Keep Description Intact",
                    StartDateTime = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
                    EndDateTime = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
                    CreatorId = user.Id,
                    CreatedOn = DateTime.UtcNow
                };
                db.Event.Add(initialEvent);
                await db.SaveChangesAsync();
                eventId = initialEvent.Id;
            }

            // Send empty multipart form data
            var content = new MultipartFormDataContent();
            var response = await client.PostAsync($"/api/v1/Events/{eventId}", content);

            // Should be rejected by FluentValidation instead of saving empty event
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            using var scope2 = _factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var eventInDb = db2.Event.FirstOrDefault(e => e.Id == eventId);

            eventInDb!.Title.Should().Be("Keep Me Intact");
            eventInDb.Description.Should().Be("Keep Description Intact");
        }
    }
}
