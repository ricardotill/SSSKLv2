using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Web.Registrations;

/// <summary>
/// Fixed users and data for the Playwright suite. Only runs in the E2E environment.
/// Keep in sync with <c>Frontend/e2e/support/users.ts</c>.
/// </summary>
internal static class E2ESeeder
{
    public const string Password = "E2e-Passw0rd!";

    private static readonly (string UserName, string Name, string Surname, string Role, decimal Saldo)[] Users =
    [
        ("e2e-admin", "Admin", "E2E", Roles.Admin, 100m),
        ("e2e-user", "Gebruiker", "E2E", Roles.User, 25m),
        ("e2e-user2", "Tweede", "E2E", Roles.User, 10m),
        ("e2e-kiosk", "Kiosk", "E2E", Roles.Kiosk, 0m),
        ("e2e-guest", "Gast", "E2E", Roles.Guest, 0m),
    ];

    private static readonly (string Name, decimal Price, int Stock)[] Products =
    [
        ("E2E Cola", 1.50m, 1000),
        ("E2E Bier", 2.00m, 1000),
        ("E2E Uitverkocht", 1.00m, 0),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        foreach (var (userName, name, surname, role, saldo) in Users)
        {
            if (await userManager.FindByNameAsync(userName) != null) continue;

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = $"{userName}@example.com",
                EmailConfirmed = true,
                Name = name,
                Surname = surname,
                Saldo = saldo,
            };
            EnsureSucceeded(await userManager.CreateAsync(user, Password), userName);
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role), userName);
        }

        foreach (var (name, price, stock) in Products)
        {
            if (await db.Product.AnyAsync(p => p.Name == name)) continue;
            db.Product.Add(new Product { Name = name, Price = price, Stock = stock });
        }

        const string eventTitle = "E2E Kampvuuravond";
        if (!await db.Event.AnyAsync(e => e.Title == eventTitle))
        {
            var admin = await userManager.FindByNameAsync("e2e-admin");
            var start = DateTime.UtcNow.Date.AddDays(14).AddHours(18);
            db.Event.Add(new Event
            {
                Title = eventTitle,
                Description = "<p>Seeded event for the E2E suite.</p>",
                StartDateTime = start,
                EndDateTime = start.AddHours(4),
                CreatorId = admin!.Id,
            });
        }

        await db.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string userName)
    {
        if (result.Succeeded) return;
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Seeding E2E user '{userName}' failed: {errors}");
    }
}
