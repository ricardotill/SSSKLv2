using Azure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Hosting;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Agents;
using SSSKL.Infrastructure.Hosting;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Infrastructure.Persistence.Repositories;
using SSSKL.Infrastructure.Services;

namespace SSSKL.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddPersistence();

        // Storage:Provider picks the store for uploaded files (see BlobStorageProvider). Local files need no
        // Azure client, so the Azure settings in appsettings.json (Storage:ServiceUri) are then ignored.
        if (BlobStorageProvider.UsesLocalFiles(builder.Configuration, builder.Environment))
        {
            builder.Services.AddSingleton<IBlobStorageAgent, LocalFileBlobStorageAgent>();
        }
        else
        {
            builder.AddBlobStorage();
            builder.Services.AddSingleton<IBlobStorageAgent, BlobStorageAgent>();
        }

        builder.Services
            .AddRepositories()
            // Implement a proper IEmailSender using SMTP
            .AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>()
            .AddTransient<IWebPushService, WebPushService>()
            .AddHttpClient()
            .AddTurnstile();

        return builder;
    }

    private static void AddPersistence(this IHostApplicationBuilder builder)
    {
        // Test environments (integration tests, E2E) run on SQLite. The connection string is read when the
        // context is built, so WebApplicationFactory configuration overrides are applied.
        if (builder.Environment.IsTestLike())
        {
            builder.Services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
            {
                var sqliteConnectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("db")
                    ?? throw new InvalidOperationException(
                        $"ConnectionStrings:db is required in the {builder.Environment.EnvironmentName} environment.");
                options.UseSqlite(sqliteConnectionString)
                    .AddInterceptors(new SqliteFunctionsInterceptor());
            });
            return;
        }

        var connectionString = (builder.Environment.IsProduction() ? builder.Configuration["AZURE_SQL_CONNECTIONSTRING"] : null)
                              ?? builder.Configuration.GetConnectionString("db")
                              ?? builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                // Retry on Azure SQL transient/throttling errors instead of failing outright
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                // Avoid Cartesian-product row bloat from multi-Include queries
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });
        builder.EnrichSqlServerDbContext<ApplicationDbContext>();
    }

    private static void AddBlobStorage(this IHostApplicationBuilder builder)
    {
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
        else
        {
            return;
        }

        // Only check blob storage once we know a BlobServiceClient is registered
        builder.Services.AddHealthChecks().AddCheck<BlobStorageHealthCheck>("blob-storage", tags: ["ready"]);
    }

    private static IServiceCollection AddTurnstile(this IServiceCollection services)
    {
        services.AddOptions<TurnstileOptions>().BindConfiguration(TurnstileOptions.SectionName);
        services.AddHttpClient<ITurnstileService, TurnstileService>(client => client.Timeout = TimeSpan.FromSeconds(10));
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .AddTransient<IApplicationUserRepository, ApplicationUserRepository>()
            .AddTransient<IOldUserMigrationRepository, OldUserMigrationRepository>()
            .AddTransient<IOrderRepository, OrderRepository>()
            .AddTransient<IProductRepository, ProductRepository>()
            .AddTransient<ITopUpRepository, TopUpRepository>()
            .AddTransient<IAnnouncementRepository, AnnouncementRepository>()
            .AddTransient<IAchievementRepository, AchievementRepository>()
            .AddTransient<IEventRepository, EventRepository>()
            .AddTransient<IQuoteRepository, QuoteRepository>()
            .AddTransient<IUserStatRepository, UserStatRepository>()
            .AddTransient<IProductUserStatRepository, ProductUserStatRepository>()
            .AddTransient<IGlobalSettingRepository, GlobalSettingRepository>()
            .AddTransient<IRoleRepository, RoleRepository>()
            .AddTransient<IBlobStorageItemRepository, BlobStorageItemRepository>()
            .AddTransient<IUserImageRepository, UserImageRepository>()
            .AddTransient<INotificationRepository, NotificationRepository>()
            .AddTransient<IPushSubscriptionRepository, PushSubscriptionRepository>()
            .AddTransient<IReactionRepository, ReactionRepository>();
    }
}
