using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public static class ServicesExtensions
{
    public static IServiceCollection AddServicesDI(this IServiceCollection services)
    {
        return services
            .AddScoped<IHeaderService, HeaderService>()
            .AddTransient<IApplicationUserService, ApplicationUserService>()
            .AddTransient<IOldUserMigrationService, OldUserMigrationService>()
            .AddTransient<IOrderService, OrderService>()
            .AddTransient<IProductService, ProductService>()
            .AddTransient<ITopUpService, TopUpService>()
            .AddTransient<IAnnouncementService, AnnouncementService>()
            .AddTransient<IAchievementService, AchievementService>()
            .AddSingleton<IPurchaseNotifier, PurchaseNotifier>();
    }
}