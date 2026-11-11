using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class AnnouncementIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private static object Announcement(string message) =>
        new { message, description = "details", order = 1, isScheduled = false };

    [TestMethod]
    public async Task GetAll_Anonymous_Returns401_User_Returns200()
    {
        var (user, _) = await CreateAuthenticatedClientAsync(Roles.User);

        (await AnonymousClient().GetAsync("/api/v1/announcement", CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await user.GetAsync("/api/v1/announcement", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Create_Update_Delete_AsAdmin()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        var created = await admin.PostAsJsonAsync("/api/v1/announcement", Announcement("Kamp gaat door"), CT);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await Describe(created));
        var id = (await ReadJsonAsync<JsonElement>(created)).GetProperty("id").GetGuid();

        var updated = await admin.PutAsJsonAsync($"/api/v1/announcement/{id}", Announcement("Kamp verplaatst"), CT);
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent, await Describe(updated));
        var fetched = await ReadJsonAsync<JsonElement>(await admin.GetAsync($"/api/v1/announcement/{id}", CT));
        fetched.GetProperty("message").GetString().Should().Be("Kamp verplaatst");

        (await admin.DeleteAsync($"/api/v1/announcement/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/v1/announcement/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Create_AsUser_Returns403()
    {
        var (user, _) = await CreateAuthenticatedClientAsync(Roles.User);

        (await user.PostAsJsonAsync("/api/v1/announcement", Announcement("Nope"), CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        (await admin.PostAsJsonAsync("/api/v1/announcement", new { message = "No order" }, CT)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Delete_Unknown_Returns404()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);

        (await admin.DeleteAsync($"/api/v1/announcement/{Guid.NewGuid()}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
