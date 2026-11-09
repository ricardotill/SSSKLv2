using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Web.Test.Integration.Infrastructure;

/// <summary>
/// Helpers for integration tests. Each derived class owns one <see cref="SssklWebApplicationFactory"/>
/// (created in its [ClassInitialize], disposed in its [ClassCleanup]) and returns it from <see cref="Factory"/>.
/// Tests within a class share the database, so give every test its own users and data.
/// </summary>
public abstract class IntegrationTestBase
{
    public const string Password = "P@ssw0rd123!";

    protected static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    public TestContext TestContext { get; set; } = null!;

    protected abstract SssklWebApplicationFactory Factory { get; }

    protected CancellationToken CT => TestContext.CancellationToken;

    protected HttpClient AnonymousClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected async Task<ApplicationUser> CreateUserAsync(string role = Roles.User, decimal saldo = 0, string? userName = null, bool emailConfirmed = true)
    {
        userName ??= $"u-{Guid.NewGuid():N}"[..10] + "@example.com";

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName.Contains('@') ? userName : $"{userName}@example.com",
            EmailConfirmed = emailConfirmed,
            Name = "Test",
            Surname = role,
            Saldo = saldo,
        };

        var created = await userManager.CreateAsync(user, Password);
        created.Succeeded.Should().BeTrue(string.Join(", ", created.Errors.Select(e => e.Description)));
        (await userManager.AddToRoleAsync(user, role)).Succeeded.Should().BeTrue();
        return user;
    }

    protected async Task<string> GetAccessTokenAsync(ApplicationUser user)
    {
        using var client = AnonymousClient();
        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new { userName = user.UserName, password = Password }, CT);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(CT));
        var body = await ReadJsonAsync<JsonElement>(response);
        return body.GetProperty("accessToken").GetString()!;
    }

    protected async Task<HttpClient> LoginAsync(ApplicationUser user)
    {
        var token = await GetAccessTokenAsync(user);
        var client = AnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected async Task<(HttpClient Client, ApplicationUser User)> CreateAuthenticatedClientAsync(string role = Roles.User, decimal saldo = 0)
    {
        var user = await CreateUserAsync(role, saldo);
        return (await LoginAsync(user), user);
    }

    protected async Task<string> CreateRoleAsync(string? name = null)
    {
        name ??= $"Role{Guid.NewGuid():N}"[..12];
        await using var scope = Factory.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        (await roleManager.CreateAsync(new IdentityRole(name))).Succeeded.Should().BeTrue();
        return name;
    }

    protected async Task AddToRoleAsync(ApplicationUser user, string role)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tracked = await userManager.FindByIdAsync(user.Id) ?? throw new InvalidOperationException("User not found");
        (await userManager.AddToRoleAsync(tracked, role)).Succeeded.Should().BeTrue();
    }

    protected Task<Product> SeedProductAsync(decimal price = 1.50m, int stock = 10, string? name = null, bool enableLeaderboard = true) =>
        WithDbAsync(async db =>
        {
            var product = new Product
            {
                Name = name ?? $"Product {Guid.NewGuid():N}"[..16],
                Price = price,
                Stock = stock,
                EnableLeaderboard = enableLeaderboard,
            };
            db.Product.Add(product);
            await db.SaveChangesAsync(CT);
            return product;
        });

    protected Task SeedGlobalSettingAsync(string key, string value) =>
        WithDbAsync(async db =>
        {
            var existing = await db.GlobalSetting.SingleOrDefaultAsync(s => s.Key == key, CT);
            if (existing is null)
                db.GlobalSetting.Add(new GlobalSetting { Key = key, Value = value });
            else
                existing.Value = value;
            await db.SaveChangesAsync(CT);
            return true;
        });

    protected async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(db);
    }

    protected Task<decimal> GetSaldoAsync(string userId) =>
        WithDbAsync(db => db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Saldo).SingleAsync(CT));

    protected Task<int> GetStockAsync(Guid productId) =>
        WithDbAsync(db => db.Product.AsNoTracking().Where(p => p.Id == productId).Select(p => p.Stock).SingleAsync(CT));

    /// <summary>Polls <paramref name="probe"/> until <paramref name="done"/> holds, for background jobs.</summary>
    protected async Task<T> PollAsync<T>(Func<Task<T>> probe, Func<T, bool> done, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (true)
        {
            var value = await probe();
            if (done(value)) return value;
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Condition was not met before the timeout.");
            await Task.Delay(100, CT);
        }
    }

    protected async Task<T> ReadJsonAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, CT))!;

    protected static async Task<string> Describe(HttpResponseMessage response) =>
        $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}";
}
