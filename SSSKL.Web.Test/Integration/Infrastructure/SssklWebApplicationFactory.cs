using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Hosting;
using SSSKL.Web.Test.Integration.Infrastructure.Fakes;

namespace SSSKL.Web.Test.Integration.Infrastructure;

/// <summary>
/// Runs the real API in the IntegrationTest environment on its own SQLite database file.
/// The app creates the schema and seeds the roles itself on first use.
/// Email, web push and blob storage are replaced by in-memory fakes.
/// </summary>
public sealed class SssklWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ssskl-it-{Guid.NewGuid():N}.db");
    private readonly Dictionary<string, string?> _settings = new();
    private readonly List<Action<IServiceCollection>> _serviceOverrides = [];
    private string _environment = TestEnvironments.IntegrationTest;

    public FakeEmailSender Emails { get; } = new();
    public FakeWebPushService Push { get; } = new();
    public InMemoryBlobStorageAgent Blobs { get; } = new();

    public SssklWebApplicationFactory()
    {
        _settings["ConnectionStrings:db"] = $"Data Source={_dbPath}";
        _settings["WEBSITE_DOMAIN"] = "https://localhost";
        _settings["Storage:ServiceUri"] = "";
        _settings["RateLimiting:SignInPermitLimit"] = "10000";
        _settings["RateLimiting:EmailPermitLimit"] = "10000";
    }

    /// <summary>Overrides a configuration value. Call before the first request.</summary>
    public SssklWebApplicationFactory WithSetting(string key, string? value)
    {
        _settings[key] = value;
        return this;
    }

    /// <summary>Runs the host in another environment, e.g. <see cref="TestEnvironments.E2E"/>. Call before the first request.</summary>
    public SssklWebApplicationFactory WithEnvironment(string environment)
    {
        _environment = environment;
        return this;
    }

    /// <summary>Applies extra service registrations after the defaults. Call before the first request.</summary>
    public SssklWebApplicationFactory WithServices(Action<IServiceCollection> configure)
    {
        _serviceOverrides.Add(configure);
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        // UseSetting is visible to code in Program.cs that reads configuration before Build().
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender<ApplicationUser>>();
            services.AddSingleton<IEmailSender<ApplicationUser>>(Emails);
            services.RemoveAll<IWebPushService>();
            services.AddSingleton<IWebPushService>(Push);
            services.RemoveAll<IBlobStorageAgent>();
            services.AddSingleton<IBlobStorageAgent>(Blobs);

            foreach (var configure in _serviceOverrides)
            {
                configure(services);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
                // Best effort: a leftover temp file doesn't affect other tests.
            }
        }
    }
}
