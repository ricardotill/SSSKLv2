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

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(180);
    options.SlidingExpiration = false;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Kiosk", policy => policy.RequireRole("Kiosk"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
    options.AddPolicy("Guest", policy => policy.RequireRole("Guest"));
});

string connection;
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
    connection = builder.Configuration.GetConnectionString("DefaultConnection") 
                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
else
{
    connection = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")
                 ?? throw new InvalidOperationException("Azure SQL Server Connection string not found.");
}

builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("SSSKLv2", LogLevel.Trace);

builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options =>
    {
        options.UseSqlServer(connection,
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 15,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
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
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<IdentityClaimsPrincipalFactory>()
    .AddApiEndpoints();

if (builder.Environment.IsDevelopment()) builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
else builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();

builder.Services.AddBlazoredToast();

builder.Services.AddServicesDI();
builder.Services.AddDataDI();
builder.Services.AddAgentsDI();

builder.Services.AddAntiforgery(o => o.SuppressXFrameOptionsHeader = true);

var app = builder.Build();

app.UsePathBase("/");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.EnsureCreatedAsync();
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
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .WithStaticAssets()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapGroup("/api")
    .MapControllers();
app.MapGroup("/api/v1/identity")
    .MapIdentityApi<IdentityUser>();


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
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

await app.RunAsync();
