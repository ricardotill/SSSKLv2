using SSSKL.Application.Events;
using SSSKL.Application.Handlers;
using SSSKL.Application.Services;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Events;

namespace SSSKL.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services
            .AddTransient<IApplicationUserService, ApplicationUserService>()
            .AddTransient<IOldUserMigrationService, OldUserMigrationService>()
            .AddTransient<IOrderService, OrderService>()
            .AddTransient<IProductService, ProductService>()
            .AddTransient<ITopUpService, TopUpService>()
            .AddTransient<IAnnouncementService, AnnouncementService>()
            .AddTransient<IAchievementService, AchievementService>()
            .AddTransient<IEventService, EventService>()
            .AddTransient<IQuoteService, QuoteService>()
            .AddTransient<IReactionService, ReactionService>()
            .AddTransient<INotificationService, NotificationService>()
            .AddSingleton<IStatsRecalculationJobService, StatsRecalculationJobService>()
            .AddSingleton<IOrderCsvExportJobService, OrderCsvExportJobService>()
            .AddScoped<IDomainEventDispatcher, DomainEventDispatcher>()
            .AddScoped<IDomainEventHandler<OrderPlacedEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<TopUpEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<QuoteCreatedEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<QuoteVotedEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<ReactionAddedEvent>, AchievementEventHandler>();
    }
}
