using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKL.Domain.Constants;
using SSSKL.Infrastructure.Hosting;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Web.Registrations;

public static class DatabaseInitialization
{
    // Applies migrations (or creates the SQLite schema in test environments) and seeds the protected roles.
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsE2E())
        {
            // Every E2E run starts from a fresh, seeded database.
            E2EGuard.EnsureNotHosted();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
        else if (app.Environment.IsIntegrationTest())
        {
            // SQLite schema for integration tests; the SQL Server migrations don't apply to it.
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.MigrateAsync();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.AllProtected)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (app.Environment.IsE2E())
        {
            // E2E:DemoData swaps the Playwright fixtures for realistic data (store screenshots).
            if (app.Configuration.GetValue<bool>("E2E:DemoData"))
                await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
            else
                await E2ESeeder.SeedAsync(scope.ServiceProvider);
        }
    }
}
