using Microsoft.Extensions.Configuration;
using SSSKL.Application.Behaviors;
using SSSKL.Application.Events;
using SSSKL.Application.Services;
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
            .AddTransient<IOrderService, OrderService>()
            .AddTransient<IAchievementService, AchievementService>()
            .AddTransient<IEventService, EventService>()
            .AddTransient<IQuoteService, QuoteService>()
            .AddTransient<IReactionService, ReactionService>()
            .AddTransient<INotificationService, NotificationService>()
            .AddSingleton<IStatsRecalculationJobService, StatsRecalculationJobService>()
            .AddSingleton<IOrderCsvExportJobService, OrderCsvExportJobService>()
            .AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    }
}
