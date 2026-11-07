using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class RolesIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private static string UniqueRole() => $"Role{Guid.NewGuid():N}"[..12];

    private async Task<string> CreateRoleAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/roles", new { name }, CT);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await Describe(response));
        return (await ReadJsonAsync<JsonElement>(response)).GetProperty("id").GetString()!;
    }

    private async Task<string> RoleIdAsync(HttpClient admin, string name)
    {
        var roles = await ReadJsonAsync<JsonElement>(await admin.GetAsync("/api/v1/roles/admin", CT));
        return roles.EnumerateArray().Single(r => r.GetProperty("name").GetString() == name).GetProperty("id").GetString()!;
    }

    [TestMethod]
    public async Task GetAll_AsUser_HidesProtectedRoles_AdminListIsForbidden()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var custom = UniqueRole();
        await CreateRoleAsync(admin, custom);
        var (user, _) = await CreateAuthenticatedClientAsync(Roles.User);

        var names = (await ReadJsonAsync<JsonElement>(await user.GetAsync("/api/v1/roles", CT)))
            .EnumerateArray().Select(r => r.GetProperty("name").GetString()).ToList();

        names.Should().Contain(custom).And.NotContain(Roles.AllProtected);
        (await user.GetAsync("/api/v1/roles/admin", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task GetAll_AsGuest_Returns403()
    {
        var (guest, _) = await CreateAuthenticatedClientAsync(Roles.Guest);

        (await guest.GetAsync("/api/v1/roles", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Create_Duplicate_Returns409_Empty_Returns400()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var name = UniqueRole();
        await CreateRoleAsync(admin, name);

        (await admin.PostAsJsonAsync("/api/v1/roles", new { name }, CT)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await admin.PostAsJsonAsync("/api/v1/roles", new { name = " " }, CT)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Update_CustomRole_Renames()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var id = await CreateRoleAsync(admin, UniqueRole());
        var renamed = UniqueRole();

        var response = await admin.PutAsJsonAsync($"/api/v1/roles/{id}", new { name = renamed }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
        (await ReadJsonAsync<JsonElement>(response)).GetProperty("name").GetString().Should().Be(renamed);
    }

    [TestMethod]
    public async Task UpdateOrDelete_ProtectedRole_Returns403()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var userRoleId = await RoleIdAsync(admin, Roles.User);

        (await admin.PutAsJsonAsync($"/api/v1/roles/{userRoleId}", new { name = UniqueRole() }, CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.DeleteAsync($"/api/v1/roles/{userRoleId}", CT)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Delete_CustomRole_Returns204_UnknownReturns404()
    {
        var (admin, _) = await CreateAuthenticatedClientAsync(Roles.Admin);
        var id = await CreateRoleAsync(admin, UniqueRole());

        (await admin.DeleteAsync($"/api/v1/roles/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"/api/v1/roles/{id}", CT)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
