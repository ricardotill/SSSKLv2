using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SSSKL.Application;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Web.Hubs;
using SSSKL.Web.Services;
using SSSKL.Web.Registrations;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Treat the IntegrationTest environment like Development for dev-friendly behaviors
// such as using SameAsRequest for cookie SecurePolicy. This prevents antiforgery
// and cookie codepaths from requiring an actual SSL request when running tests
// under TestServer.
var isIntegrationLikeEnvironment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("IntegrationTest");
var websiteDomain = builder.Configuration["WEBSITE_DOMAIN"] ?? (builder.Environment.IsDevelopment() ? "localhost" : "ssskl.scoutingwilo.nl");

builder.Services.AddControllers();

// Uniform RFC 7807 error responses; unhandled exceptions are mapped in GlobalExceptionHandler.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Per-IP limits on login and email-sending identity endpoints
builder.Services.AddRateLimitingRegistrations();

// Register all FluentValidation validators from the application assembly
builder.Services.AddFluentValidationsRegistrations();

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
    "http://localhost:4200",
    "https://localhost:4200",
    "http://localhost:5173",
    "https://localhost:5173",
    "https://sssklv2-app.azurewebsites.net",
    $"https://{websiteDomain}",
    "https://ambitious-desert-07f83a203.2.azurestaticapps.net",
    "https://ssskl.ricardotill.nl"
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

// Persistence, blob storage, repositories, email, web push and Turnstile.
// The "self" liveness check comes from AddServiceDefaults, the SQL check from EnrichSqlServerDbContext
// and the blob storage check from AddInfrastructure once a BlobServiceClient is registered.
builder.AddInfrastructure();

if (builder.Environment.IsProduction())
{
    // Registration doesn't connect; the Azure SignalR SDK reconnects to the service on its own.
    builder.Services.AddSignalR().AddAzureSignalR(options =>
    {
        options.ServerStickyMode = Microsoft.Azure.SignalR.ServerStickyMode.Required;
    });
}

if (isIntegrationLikeEnvironment)
{
    builder.Services.AddSignalR();
}

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddApiEndpoints();

builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    options.ServerDomain = websiteDomain;
});

builder.Services.AddMemoryCache();
builder.Services.AddApplication();
builder.Services
    .AddSingleton<IPurchaseNotifier, PurchaseNotifier>()
    .AddSingleton<IEventNotifier, EventNotifier>();

// For now disabled, CPU-intensive
// builder.Services.AddResponseCompression(options =>
// {
//     options.EnableForHttps = true;
//     // Default safe MIME types are used by default.
// });

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-XSRF-TOKEN";
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

app.UseForwardedHeaders();
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
    app.UseExceptionHandler();
    app.UseHsts();
}



// app.UseResponseCompression();
app.UseHttpsRedirection();
// Apply cookie policy before authentication so cookie flags are enforced.
app.UseCookiePolicy();

// Global middleware to provide the XSRF token to the frontend
app.Use(async (context, next) =>
{
    // Skip antiforgery for blob image requests to avoid Set-Cookie headers that prevent CDN caching
    if (context.Request.Path.StartsWithSegments("/api/v1/blob"))
    {
        await next(context);
        return;
    }

    var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(context);

    if (tokens.RequestToken != null)
    {
        context.Response.Cookies.Append(
            "XSRF-TOKEN",
            tokens.RequestToken,
            new CookieOptions
            {
                HttpOnly = false, // Angular needs to read this cookie
                Secure = !isIntegrationLikeEnvironment,
                SameSite = SameSiteMode.Lax
            });
    }

    await next(context);
});

// Apply CORS policy.
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

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
app.MapHub<LiveMetricsHub>("/hubs/livemetrics");

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

// /healthz: everything (database, blob storage). /healthz/live: process only, for restart decisions,
// so a database outage doesn't make the platform recycle otherwise healthy instances.
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

await app.RunAsync();

// Expose the Program type for WebApplicationFactory in tests
public partial class Program { }
