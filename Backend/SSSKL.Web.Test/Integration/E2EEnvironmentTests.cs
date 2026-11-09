using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Hosting;
using SSSKL.Web.Registrations;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

/// <summary>
/// Boots the API in the E2E environment, which recreates the database and seeds the Playwright fixtures.
/// </summary>
[TestClass]
public class E2EEnvironmentTests : IntegrationTestBase
{
    private const string SeedPassword = "Seed-Passw0rd!";

    private static SssklWebApplicationFactory _factory = null!;

    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _) =>
        _factory = new SssklWebApplicationFactory()
            .WithEnvironment(TestEnvironments.E2E)
            .WithSetting("E2E:UserPassword", SeedPassword);

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestMethod]
    [DataRow("e2e-admin", Roles.Admin)]
    [DataRow("e2e-user", Roles.User)]
    [DataRow("e2e-user2", Roles.User)]
    [DataRow("e2e-kiosk", Roles.Kiosk)]
    [DataRow("e2e-guest", Roles.Guest)]
    public async Task Startup_SeedsUsersWithRoles(string userName, string role)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(userName);

        user.Should().NotBeNull();
        user!.EmailConfirmed.Should().BeTrue();
        (await userManager.IsInRoleAsync(user, role)).Should().BeTrue();
        (await userManager.CheckPasswordAsync(user, SeedPassword)).Should().BeTrue();
    }

    [TestMethod]
    public async Task Startup_SeedsProductsAndEvent()
    {
        var products = await WithDbAsync(db => db.Product.Where(p => p.Name.StartsWith("E2E ")).ToListAsync(CT));
        var events = await WithDbAsync(db => db.Event.CountAsync(e => e.Title == "E2E Kampvuuravond", CT));

        products.Select(p => p.Name).Should().BeEquivalentTo("E2E Cola", "E2E Bier", "E2E Uitverkocht");
        products.Single(p => p.Name == "E2E Uitverkocht").Stock.Should().Be(0);
        events.Should().Be(1);
    }

    [TestMethod]
    public async Task SeededUser_CanLogIn()
    {
        using var client = AnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new { userName = "e2e-user", password = SeedPassword }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(response));
    }

    [TestMethod]
    public async Task SeedAsync_RunTwice_AddsNoDuplicates()
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            await E2ESeeder.SeedAsync(scope.ServiceProvider);
        }

        (await WithDbAsync(db => db.Users.CountAsync(u => u.UserName!.StartsWith("e2e-"), CT))).Should().Be(5);
        (await WithDbAsync(db => db.Product.CountAsync(p => p.Name.StartsWith("E2E "), CT))).Should().Be(3);
        (await WithDbAsync(db => db.Event.CountAsync(e => e.Title == "E2E Kampvuuravond", CT))).Should().Be(1);
    }
}

/// <summary>Startup failures of the E2E seeding. Each test owns its factory because startup throws.</summary>
[TestClass]
public class E2EEnvironmentStartupFailureTests
{
    [TestMethod]
    public void Startup_WithoutUserPassword_Fails()
    {
        using var factory = new SssklWebApplicationFactory()
            .WithEnvironment(TestEnvironments.E2E)
            .WithSetting("E2E:UserPassword", "");

        var act = () => factory.Services;

        act.Should().Throw<InvalidOperationException>().WithMessage("*E2E:UserPassword*");
    }

    [TestMethod]
    public void Startup_WithPasswordRejectedByIdentity_Fails()
    {
        using var factory = new SssklWebApplicationFactory()
            .WithEnvironment(TestEnvironments.E2E)
            .WithSetting("E2E:UserPassword", "short");

        var act = () => factory.Services;

        act.Should().Throw<InvalidOperationException>().WithMessage("Seeding E2E user 'e2e-admin' failed*");
    }
}
