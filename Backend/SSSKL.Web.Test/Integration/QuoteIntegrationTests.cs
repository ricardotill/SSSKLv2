using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class QuoteIntegrationTests : IntegrationTestBase
{
    private const string FeatureRolesKey = "QuotesFeatureAllowedRoles";

    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    // Tests that restrict the feature set the global setting themselves; make sure it's cleared for the others.
    [TestInitialize]
    public Task ClearFeatureRestriction() => SeedGlobalSettingAsync(FeatureRolesKey, "");

    private static object NewQuote(string text, IEnumerable<string>? authorIds = null, IEnumerable<string>? visibleToRoles = null) => new
    {
        text,
        dateSaid = new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc),
        authors = (authorIds ?? []).Select(id => new { applicationUserId = id }).ToArray(),
        visibleToRoles = visibleToRoles ?? [],
        sendNotification = false,
    };

    private async Task<Guid> CreateQuoteAsync(HttpClient client, string text = "Wie dit leest is gek", IEnumerable<string>? authorIds = null, IEnumerable<string>? visibleToRoles = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quote", NewQuote(text, authorIds, visibleToRoles), CT);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await Describe(response));
        return (await ReadJsonAsync<JsonElement>(response)).GetProperty("id").GetGuid();
    }

    private static object Update(string text) => new { text, dateSaid = DateTime.UtcNow.Date, authors = Array.Empty<object>(), visibleToRoles = Array.Empty<string>() };

    [TestMethod]
    public async Task Create_WithAuthor_IsReturnedByGetById()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var author = await CreateUserAsync(Roles.User);

        var id = await CreateQuoteAsync(client, "Ik heb niks gedaan", [author.Id]);

        var quote = await ReadJsonAsync<JsonElement>(await client.GetAsync($"/api/v1/quote/{id}", CT));
        quote.GetProperty("text").GetString().Should().Be("Ik heb niks gedaan");
        quote.GetProperty("authors").GetArrayLength().Should().Be(1);
    }

    [TestMethod]
    public async Task Create_MissingDateSaid_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);

        var response = await client.PostAsJsonAsync("/api/v1/quote", new { text = "Geen datum", sendNotification = false }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Create_Anonymous_Returns401_GetAllAnonymous_Returns200()
    {
        (await AnonymousClient().PostAsJsonAsync("/api/v1/quote", NewQuote("Anoniem"), CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await AnonymousClient().GetAsync("/api/v1/quote", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Vote_TogglesOnAndOff()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateQuoteAsync(client);

        (await ReadJsonAsync<bool>(await client.PostAsync($"/api/v1/quote/{id}/vote", null, CT))).Should().BeTrue();
        var voted = await ReadJsonAsync<JsonElement>(await client.GetAsync($"/api/v1/quote/{id}", CT));
        voted.GetProperty("voteCount").GetInt32().Should().Be(1);
        voted.GetProperty("hasVoted").GetBoolean().Should().BeTrue();

        (await ReadJsonAsync<bool>(await client.PostAsync($"/api/v1/quote/{id}/vote", null, CT))).Should().BeFalse();
        (await ReadJsonAsync<JsonElement>(await client.GetAsync($"/api/v1/quote/{id}", CT))).GetProperty("voteCount").GetInt32().Should().Be(0);
    }

    [TestMethod]
    public async Task Vote_UnknownQuote_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClientAsync(Roles.User);

        (await client.PostAsync($"/api/v1/quote/{Guid.NewGuid()}/vote", null, CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Update_CreatorAuthorAndAdminMay_StrangerIsForbidden()
    {
        var (creator, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (author, authorUser) = await CreateAuthenticatedClientAsync(Roles.User);
        var (stranger, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var id = await CreateQuoteAsync(creator, authorIds: [authorUser.Id]);

        (await stranger.PutAsJsonAsync($"/api/v1/quote/{id}", Update("Stranger"), CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await creator.PutAsJsonAsync($"/api/v1/quote/{id}", Update("Creator"), CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        // The creator's update cleared the authors, so re-add the author before checking author access.
        (await admin.PutAsJsonAsync($"/api/v1/quote/{id}",
            new { text = "Admin", dateSaid = DateTime.UtcNow.Date, authors = new[] { new { applicationUserId = authorUser.Id } }, visibleToRoles = Array.Empty<string>() }, CT))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await author.PutAsJsonAsync($"/api/v1/quote/{id}",
            new { text = "Author", dateSaid = DateTime.UtcNow.Date, authors = new[] { new { applicationUserId = authorUser.Id } }, visibleToRoles = Array.Empty<string>() }, CT))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReadJsonAsync<JsonElement>(await admin.GetAsync($"/api/v1/quote/{id}", CT))).GetProperty("text").GetString().Should().Be("Author");
    }

    [TestMethod]
    public async Task Delete_ByCreator_Returns204_ThenGetReturns404()
    {
        var (creator, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (stranger, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateQuoteAsync(creator);

        (await stranger.DeleteAsync($"/api/v1/quote/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await creator.DeleteAsync($"/api/v1/quote/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await creator.GetAsync($"/api/v1/quote/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task VisibleToRoles_HidesQuoteFromNonMembers()
    {
        var role = await CreateRoleAsync();
        var (member, memberUser) = await CreateAuthenticatedClientAsync(Roles.User);
        await AddToRoleAsync(memberUser, role);
        var (outsider, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var id = await CreateQuoteAsync(member, visibleToRoles: [role]);

        (await member.GetAsync($"/api/v1/quote/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await outsider.GetAsync($"/api/v1/quote/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AnonymousClient().GetAsync($"/api/v1/quote/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task FeatureRestricted_UserWithoutRoleIsForbidden_MemberAndAdminAllowed()
    {
        var role = await CreateRoleAsync();
        var (member, memberUser) = await CreateAuthenticatedClientAsync(Roles.User);
        await AddToRoleAsync(memberUser, role);
        var (outsider, _) = await CreateAuthenticatedClientAsync(Roles.User);
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        await SeedGlobalSettingAsync(FeatureRolesKey, role);

        (await outsider.GetAsync("/api/v1/quote", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await AnonymousClient().GetAsync("/api/v1/quote", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await member.GetAsync("/api/v1/quote", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync("/api/v1/quote", CT)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
