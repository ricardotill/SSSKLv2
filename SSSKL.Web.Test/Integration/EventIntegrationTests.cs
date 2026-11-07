using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class EventIntegrationTests : IntegrationTestBase
{
    // Smallest valid PNG (1x1 transparent pixel).
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private static MultipartFormDataContent EventForm(string title = "Opkomst", string description = "<p>Gezellig</p>", IEnumerable<string>? requiredRoles = null)
    {
        var start = DateTime.UtcNow.Date.AddDays(7).AddHours(19);
        var form = new MultipartFormDataContent
        {
            { new StringContent(title), "Title" },
            { new StringContent(description), "Description" },
            { new StringContent(start.ToString("yyyy-MM-ddTHH:mm:ssZ")), "StartDateTime" },
            { new StringContent(start.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ssZ")), "EndDateTime" },
        };
        foreach (var role in requiredRoles ?? [])
        {
            form.Add(new StringContent(role), "RequiredRoles");
        }
        return form;
    }

    private static ByteArrayContent File(byte[] bytes, string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }

    private async Task<Guid> CreateEventAsync(HttpClient client, MultipartFormDataContent? form = null)
    {
        var response = await client.PostAsync("/api/v1/events", form ?? EventForm(), CT);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await Describe(response));
        return (await ReadJsonAsync<JsonElement>(response)).GetProperty("id").GetGuid();
    }

    private Task<Event> LoadEventAsync(Guid id) =>
        WithDbAsync(db => db.Event.AsNoTracking().SingleAsync(e => e.Id == id, CT));

    [TestMethod]
    public async Task Create_WithoutImage_Returns201_AndSanitizesDescription()
    {
        var (client, user) = await CreateAuthenticatedClientAsync(Roles.User);

        var id = await CreateEventAsync(client, EventForm(description: "<p>Hoi</p><script>alert(1)</script>"));

        var stored = await LoadEventAsync(id);
        stored.CreatorId.Should().Be(user.Id);
        stored.Description.Should().Contain("Hoi").And.NotContain("script");
    }

    [TestMethod]
    public async Task Create_WithPngImage_StoresBlobAndExposesImageUrl()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var blobsBefore = Factory.Blobs.Blobs.Count;
        var form = EventForm();
        form.Add(File(Png, "image/png"), "image", "pixel.png");

        var id = await CreateEventAsync(client, form);

        Factory.Blobs.Blobs.Count.Should().Be(blobsBefore + 1);
        var dto = await ReadJsonAsync<JsonElement>(await client.GetAsync($"/api/v1/events/{id}", CT));
        dto.GetProperty("imageUrl").GetString().Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Create_UnsupportedImage_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var form = EventForm();
        form.Add(File("not an image"u8.ToArray(), "text/plain"), "image", "notes.txt");

        var response = await client.PostAsync("/api/v1/events", form, CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Create_AsKiosk_Returns403()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.Kiosk);

        (await client.PostAsync("/api/v1/events", EventForm(), CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Update_ViaPostMultipart_UpdatesEvent()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateEventAsync(client);

        var response = await client.PostAsync($"/api/v1/events/{id}", EventForm("Nieuwe titel", "Nieuwe omschrijving"), CT);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await Describe(response));
        var stored = await LoadEventAsync(id);
        stored.Title.Should().Be("Nieuwe titel");
        stored.Description.Should().Be("Nieuwe omschrijving");
    }

    [TestMethod]
    public async Task Update_WithEmptyForm_Returns400_AndKeepsEvent()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateEventAsync(client, EventForm("Blijft staan"));

        var response = await client.PostAsync($"/api/v1/events/{id}", new MultipartFormDataContent(), CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LoadEventAsync(id)).Title.Should().Be("Blijft staan");
    }

    [TestMethod]
    public async Task Update_ByNonCreator_Returns403()
    {
        var (creator, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (other, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateEventAsync(creator);

        (await other.PutAsync($"/api/v1/events/{id}", EventForm("Gekaapt"), CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Delete_ByCreator_Returns204_UnknownReturns404()
    {
        var (creator, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (other, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateEventAsync(creator);

        (await other.DeleteAsync($"/api/v1/events/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await creator.DeleteAsync($"/api/v1/events/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await creator.DeleteAsync($"/api/v1/events/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Rsvp_Accept_ListsUserAsAccepted()
    {
        var (creator, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (guest, guestUser) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateEventAsync(creator);

        var response = await guest.PostAsJsonAsync($"/api/v1/events/{id}/rsvp", new { status = (int)EventResponseStatus.Accepted }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        var dto = await ReadJsonAsync<JsonElement>(await guest.GetAsync($"/api/v1/events/{id}", CT));
        dto.GetProperty("acceptedUsers").EnumerateArray().Select(u => u.GetProperty("userId").GetString()).Should().Contain(guestUser.Id);
    }

    [TestMethod]
    public async Task Rsvp_WithoutRequiredRole_Returns403()
    {
        var role = await CreateRoleAsync();
        var (creator, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (outsider, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (member, memberUser) = await CreateAuthenticatedClientAsync(Roles.User);
        await AddToRoleAsync(memberUser, role);
        var id = await CreateEventAsync(creator, EventForm(requiredRoles: [role]));
        var accept = new { status = (int)EventResponseStatus.Accepted };

        (await outsider.PostAsJsonAsync($"/api/v1/events/{id}/rsvp", accept, CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await member.PostAsJsonAsync($"/api/v1/events/{id}/rsvp", accept, CT)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Public_IsAnonymous_ListIsNot()
    {
        (await AnonymousClient().GetAsync("/api/v1/events/public", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await AnonymousClient().GetAsync("/api/v1/events", CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
