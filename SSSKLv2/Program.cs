using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Components;
using SSSKLv2.Data;
using System.Globalization;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Scalar.AspNetCore;
using SSSKLv2.Agents;
using SSSKLv2.Data.DAL;
using SSSKLv2.Registrations;
using SSSKLv2.Services;
using Microsoft.OpenApi;
using Microsoft.Azure.SignalR.Common;
using Blazored.Toast;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using SSSKLv2.Util;
using SSSKLv2.Data.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Treat the IntegrationTest environment like Development for dev-friendly behaviors
// such as using SameAsRequest for cookie SecurePolicy. This prevents antiforgery
// and cookie codepaths from requiring an actual SSL request when running tests
// under TestServer.
var isIntegrationLikeEnvironment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("IntegrationTest");


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

builder.Services.AddHealthChecks();

var authBuilder = builder.Services.AddAuthentication(options =>
{
    // Policy scheme as default: forwards to bearer when an Authorization header is present,
    // otherwise falls back to the Identity cookie handler.
    options.DefaultScheme = "BearerOrCookie";
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

authBuilder.AddIdentityCookies();
authBuilder.AddBearerToken(IdentityConstants.BearerScheme, options =>
{
    options.RefreshTokenExpiration = TimeSpan.FromDays(180);
});

// Inspect the Authorization header at runtime to pick the correct handler.
authBuilder.AddPolicyScheme("BearerOrCookie", "Bearer or Cookie", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        string? authorization = context.Request.Headers.Authorization;
        if (authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            return IdentityConstants.BearerScheme;
        return IdentityConstants.ApplicationScheme;
    };
});

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

    // For API paths, return 401/403 instead of redirecting to the login page.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
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
    options.AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(Roles.Kiosk, policy => policy.RequireRole(Roles.Kiosk));
    options.AddPolicy(Roles.User, policy => policy.RequireRole(Roles.User));
    options.AddPolicy(Roles.Guest, policy => policy.RequireRole(Roles.Guest));
});

// Telemetry logging is handled via OpenTelemetry in AddServiceDefaults.

// Register a CORS policy for frontend clients.
var frontendOrigins = new[]
{
    "http://localhost:3000",
    "https://localhost:3000",
    "http://localhost:5173",
    "https://localhost:5173",
    "https://ssskl.scoutingwilo.nl",
    "https://icy-island-07ad9b303.azurestaticapps.net"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
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

if (!builder.Environment.IsEnvironment("IntegrationTest"))
{
    var connectionString = (builder.Environment.IsProduction() ? builder.Configuration["AZURE_SQL_CONNECTIONSTRING"] : null)
                          ?? builder.Configuration.GetConnectionString("db");

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
    builder.EnrichSqlServerDbContext<ApplicationDbContext>();
}

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

var storageSection = builder.Configuration.GetSection("Storage");
var storageConnectionString = storageSection["ConnectionString"];
var storageServiceUri = storageSection["ServiceUri"];

var connectionStringBlobs = builder.Configuration.GetConnectionString("blobs");

if (!string.IsNullOrWhiteSpace(connectionStringBlobs))
{
    // Default Aspire-compatible behavior, looking for a connection named "blobs"
    builder.AddAzureBlobServiceClient("blobs");
}
else if (!string.IsNullOrWhiteSpace(storageConnectionString))
{
    builder.Services.AddAzureClients(clientBuilder => clientBuilder.AddBlobServiceClient(storageConnectionString));
}
else if (!string.IsNullOrWhiteSpace(storageServiceUri))
{
    builder.Services.AddAzureClients(clientBuilder =>
    {
        clientBuilder.AddBlobServiceClient(new Uri(storageServiceUri));
        clientBuilder.UseCredential(new DefaultAzureCredential());
    });
}

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddApiEndpoints();

// TODO: Implement a proper IEmailSender if needed for the API
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, NoOpEmailSender>();

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

app.MapDefaultEndpoints();

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

// Redirect all domains to the main domain specified in WEBSITE_DOMAIN environment variable
var mainDomain = builder.Configuration["WEBSITE_DOMAIN"];
if (!string.IsNullOrWhiteSpace(mainDomain))
{
    app.Use(async (context, next) =>
    {
        var host = context.Request.Host.Host;
        if (!string.Equals(host, mainDomain, StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            var request = context.Request;
            var destination = $"https://{mainDomain}{request.Path}{request.QueryString}";
            context.Response.Redirect(destination, permanent: true);
            return;
        }
        await next();
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<SocialPreviewMiddleware>();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
// Apply cookie policy before authentication so cookie flags are enforced.
app.UseCookiePolicy();

// Apply CORS policy.
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGroup("/api")
    .MapControllers();
app.MapGroup("/api/v1/identity")
    .MapSssklIdentityApi();

app.MapFallbackToFile("index.html");

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
    var roles = Roles.AllProtected;
 
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
