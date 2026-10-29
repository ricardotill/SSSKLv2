using Microsoft.Extensions.Configuration;
using SSSKL.Application.Behaviors;
using SSSKL.Application.Events;
using SSSKL.Application.Features.Orders.Export;
using SSSKL.Application.Features.Quotes;
using SSSKL.Application.Services;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Services;

namespace SSSKL.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MediatR:LicenseKey"];
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.NotificationPublisherType = typeof(ResilientNotificationPublisher);
        });

        return services
            .AddTransient<IApplicationUserService, ApplicationUserService>()
            .AddTransient<INotificationService, NotificationService>()
            .AddSingleton<IStatsRecalculationJobService, StatsRecalculationJobService>()
            .AddSingleton<OrderCsvExportJob>()
            .AddTransient<IUserNotifier, UserNotifier>()
            .AddTransient<IAchievementAwarder, AchievementAwarder>()
            .AddTransient<IUserRoleProvider, UserRoleProvider>()
            .AddTransient<QuoteAccess>()
            .AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    }
}
