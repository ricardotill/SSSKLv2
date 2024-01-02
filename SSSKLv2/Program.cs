﻿using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Components;
using SSSKLv2.Components.Account;
using SSSKLv2.Data;
using System.Globalization;
using Blazored.Toast;
using Microsoft.Azure.SignalR.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using SSSKLv2.Data.DAL;
using SSSKLv2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Kiosk", policy => policy.RequireRole("Kiosk"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
});

var connection = "";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
    connection = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
else
{
    connection = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
}

builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("SSSKLv2", LogLevel.Trace);

Policy.Handle<SqlException>()
    .WaitAndRetry(Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5),
        (ex, t, retryCount, c) => { Console.WriteLine($"Failed Attempt {retryCount}: {ex.GetType().Name}"); })
    .Execute(() =>
    {
        builder.Services.AddDbContextFactory<ApplicationDbContext>(
            options =>
                options.UseSqlServer(connection));
    });

if (builder.Environment.IsProduction())
{
    Policy.Handle<AzureSignalRException>()
        .WaitAndRetry(Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5),
            (ex, t, retryCount, c) => { Console.WriteLine($"Failed Attempt {retryCount}: {ex.GetType().Name}"); })
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


builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<IdentityClaimsPrincipalFactory>();

if (builder.Environment.IsDevelopment()) builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
else builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();

builder.Services.AddBlazoredToast();

builder.Services.AddServicesDI();
builder.Services.AddDataDI(); 

var app = builder.Build();

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

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Adding roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "User", "Kiosk" };
 
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

using (var scope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}

app.Run();
