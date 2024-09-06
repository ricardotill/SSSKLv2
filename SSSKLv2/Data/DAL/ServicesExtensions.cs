using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Data.DAL;

public static class ServicesExtensions
{
    public static IServiceCollection AddDataDI(this IServiceCollection services)
    {
        return services
            .AddTransient<IApplicationUserRepository, ApplicationUserRepository>()
            .AddTransient<IOldUserMigrationRepository, OldUserMigrationRepository>()
            .AddTransient<IOrderRepository, OrderRepository>()
            .AddTransient<IProductRepository, ProductRepository>()
            .AddTransient<ITopUpRepository, TopUpRepository>()
            .AddTransient<IAnnouncementRepository, AnnouncementRepository>()
            .AddTransient<IAchievementRepository, AchievementRepository>();
    }
}