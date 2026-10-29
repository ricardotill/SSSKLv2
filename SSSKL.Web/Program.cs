using System.Globalization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.SignalR;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SSSKL.Application;
using SSSKL.Contracts.Services;
using SSSKL.Infrastructure;
using SSSKL.Web.Hubs;
using SSSKL.Web.Registrations;
using SSSKL.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry, service discovery, resilience and the "self" health check.
builder.AddServiceDefaults();

// Treat the IntegrationTest environment like Development for dev-friendly behaviors such as
// SameAsRequest cookies, so antiforgery and cookie code paths don't require SSL under TestServer.
var isIntegrationLikeEnvironment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("IntegrationTest");
var cookieSecurePolicy = isIntegrationLikeEnvironment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
var websiteDomain = builder.Configuration["WEBSITE_DOMAIN"] ?? (builder.Environment.IsDevelopment() ? "localhost" : "ssskl.scoutingwilo.nl");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
}

builder.Services.AddControllers();

// Uniform RFC 7807 error responses; unhandled exceptions are mapped in GlobalExceptionHandler.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Per-IP limits on login and email-sending identity endpoints
builder.Services.AddRateLimitingRegistrations();
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

builder.Services.AddAuthenticationRegistrations(cookieSecurePolicy, websiteDomain);
builder.Services.AddAntiforgeryRegistrations(cookieSecurePolicy);
builder.Services.AddCorsRegistrations(websiteDomain);

// Persistence, blob storage, repositories, email, web push and Turnstile.
// The SQL health check comes from EnrichSqlServerDbContext and the blob storage check from
// AddInfrastructure once a BlobServiceClient is registered.
builder.AddInfrastructure();

builder.Services.AddMemoryCache();
builder.Services.AddApplication(builder.Configuration);

if (builder.Environment.IsProduction())
{
    // Registration doesn't connect; the Azure SignalR SDK reconnects to the service on its own.
    builder.Services.AddSignalR().AddAzureSignalR(options =>
    {
        options.ServerStickyMode = ServerStickyMode.Required;
    });
}
else if (isIntegrationLikeEnvironment)
{
    builder.Services.AddSignalR();
}

builder.Services
    .AddSingleton<IPurchaseNotifier, PurchaseNotifier>()
    .AddSingleton<IEventNotifier, EventNotifier>();

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.MapDefaultEndpoints();

app.UseForwardedHeaders();
app.UsePathBase("/");

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
// Apply cookie policy before authentication so cookie flags are enforced.
app.UseCookiePolicy();
app.UseXsrfTokenCookie(secureCookie: !isIntegrationLikeEnvironment);
app.UseCors(CorsRegistrations.FrontendPolicy);
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

app.MapHub<LiveMetricsHub>("/hubs/livemetrics");

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
