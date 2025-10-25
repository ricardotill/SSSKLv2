using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Components;
using SSSKLv2.Components.Account;
using SSSKLv2.Data;
using System.Globalization;
using Azure.Identity;
using Blazored.Toast;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.SignalR.Common;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Scalar.AspNetCore;
using SSSKLv2.Agents;
using SSSKLv2.Data.DAL;
using SSSKLv2.Registrations;
using SSSKLv2.Services;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Treat the IntegrationTest environment like Development for dev-friendly behaviors
// such as using SameAsRequest for cookie SecurePolicy. This prevents antiforgery
// and cookie codepaths from requiring an actual SSL request when running tests
// under TestServer.
var isIntegrationLikeEnvironment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("IntegrationTest");

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddControllers();

// Register all FluentValidation validators from this assembly
builder.Services.AddFluentAssertionsRegistrations();

builder.Services.AddOpenApi(opt =>
{
    opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    opt.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Contact = new OpenApiContact
        {
            Name = "Scouting Wilo",
            Email = "webmaster@scoutingwilo.nl"
        };
        return Task.CompletedTask;
    });
});


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddHealthChecks();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

// Configure application and identity-related cookies to be HttpOnly and use a secure policy.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(180);
    options.SlidingExpiration = false;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isIntegrationLikeEnvironment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// External and two-factor cookies should also be HttpOnly and secure.
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isIntegrationLikeEnvironment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Kiosk", policy => policy.RequireRole("Kiosk"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
    options.AddPolicy("Guest", policy => policy.RequireRole("Guest"));
});

builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("SSSKLv2", LogLevel.Trace);

// Allow local frontend dev servers to access the API (needed in dev to avoid CORS errors when calling Identity endpoints)
var frontendOrigins = new[]
{
    "http://localhost:3000",
    "https://localhost:3000",
    "http://localhost:5173",
    "https://localhost:5173",
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// If running in Development, register the Database Developer Page exception filter and load Development config
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options =>
    {
        // If running under the IntegrationTest environment, use InMemory database to keep tests isolated.
        if (builder.Environment.IsEnvironment("IntegrationTest"))
        {
            // // Register the InMemory provider with its own internal service provider so its EF services
            // // are not mixed into the application's root service provider. This avoids the "multiple
            // // database providers registered" error when tests exercise the host with a different
            // // provider than production.
            // var localServices = new ServiceCollection();
            // localServices.AddEntityFrameworkInMemoryDatabase();
            // var localProvider = localServices.BuildServiceProvider();

            options.UseInMemoryDatabase("IntegrationTestDb");
            // .UseInternalServiceProvider(localProvider);
        }
        else
        {
            string connection;
            if (builder.Environment.IsDevelopment())
            {
                // Development-time services/config already registered above; just read the connection string.
                connection = builder.Configuration.GetConnectionString("DefaultConnection") 
                             ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            else
            {
                connection = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")
                             ?? throw new InvalidOperationException("Azure SQL Server Connection string not found.");
            }
            options.UseSqlServer(connection,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 15,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });
        }
    });

if (builder.Environment.IsProduction())
{
    Policy.Handle<AzureSignalRException>()
        .WaitAndRetry(Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5),
            (ex, _, retryCount, _) => { Console.WriteLine($"Failed Attempt {retryCount}: {ex.GetType().Name}"); })
        .Execute(() =>
        {
            builder.Services.AddSignalR().AddAzureSignalR(options =>
            {
                options.ServerStickyMode =
                    Microsoft.Azure.SignalR.ServerStickyMode.Required;
            });
        });
}

builder.Services.AddQuickGridEntityFrameworkAdapter();

// Register SignalR so the app can inject IHubContext and map hubs.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSignalR();
}

builder.Services.AddAzureClients(clientBuilder =>
{
    // Register BlobServiceClient with a dev-friendly configuration.
    // For local development (Azurite) prefer a connection string or the emulator alias.
    var storageSection = builder.Configuration.GetSection("Storage");
    var connectionString = storageSection["ConnectionString"];
    var serviceUri = storageSection["ServiceUri"];

    if (builder.Environment.IsDevelopment())
    {
        // Prefer an explicit connection string set in appsettings.Development.json.
        // If none is provided, fall back to the Storage Emulator alias which works with Azurite/Storage Emulator.
        var devConn = !string.IsNullOrWhiteSpace(connectionString) ? connectionString : "UseDevelopmentStorage=true";
        clientBuilder.AddBlobServiceClient(devConn);
    }
    else
    {
        // In production, prefer a ServiceUri with DefaultAzureCredential or an explicit connection string.
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            clientBuilder.AddBlobServiceClient(connectionString);
        }
        else if (!string.IsNullOrWhiteSpace(serviceUri))
        {
            clientBuilder.AddBlobServiceClient(new Uri(serviceUri));
            clientBuilder.UseCredential(new DefaultAzureCredential());
        }
    }
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // During integration tests we create users via the API and don't want to require
        // email confirmation flows. Use the isIntegrationLikeEnvironment flag to disable
        // RequireConfirmedAccount for IntegrationTest and Development while keeping it enabled
        // in Production.
        options.SignIn.RequireConfirmedAccount = !isIntegrationLikeEnvironment;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<IdentityClaimsPrincipalFactory>()
    .AddApiEndpoints();

if (isIntegrationLikeEnvironment) builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
else builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();

builder.Services.AddBlazoredToast();

builder.Services.AddServicesDI();
builder.Services.AddDataDI();
builder.Services.AddAgentsDI();

builder.Services.AddAntiforgery(o =>
{
    o.SuppressXFrameOptionsHeader = true;
    // Make antiforgery cookie HttpOnly and secure.
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = isIntegrationLikeEnvironment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure a global cookie policy to enforce HttpOnly and secure cookies where possible.
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = isIntegrationLikeEnvironment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

var app = builder.Build();

// Diagnostic: when running integration tests, log the concrete IEmailSender implementation to help debug which
// email sender implementation was registered.
if (isIntegrationLikeEnvironment)
{
    using var _diagScope = app.Services.CreateScope();
    try
    {
        var _emailSender = _diagScope.ServiceProvider.GetService<IEmailSender<ApplicationUser>>();
        Console.WriteLine($"DI Diagnostic: IEmailSender<ApplicationUser> = {(_emailSender?.GetType().FullName ?? "<none>")}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DI Diagnostic: failed to resolve IEmailSender<ApplicationUser>: {ex.GetType().FullName}: {ex.Message}");
    }
}

app.UsePathBase("/");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!app.Environment.IsEnvironment("IntegrationTest"))
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        // In-memory provider used for integration tests does not support migrations; ensure DB is created instead.
        await db.Database.EnsureCreatedAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
// Apply cookie policy before authentication so cookie flags are enforced.
app.UseCookiePolicy();

// Apply CORS policy for frontend during development so client can call APIs with credentials (cookies)
app.UseCors("AllowLocalFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .WithStaticAssets()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapGroup("/api")
    .MapControllers();
app.MapGroup("/api/v1/identity")
    .MapSssklIdentityApi();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Map our SignalR hub for user purchases
app.MapHub<SSSKLv2.Services.Hubs.LiveMetricsHub>("/hubs/livemetrics");

// Adding roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "User", "Kiosk", "Guest" };
 
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

app.MapHealthChecks("/healthz");

CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("nl-NL");
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

using (var scope = app.Services.GetService<IServiceScopeFactory>()!.CreateScope())
{
    if (!app.Environment.IsEnvironment("IntegrationTest"))
    {
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }
}

await app.RunAsync();

// Expose the Program type for WebApplicationFactory in tests
public partial class Program { }
