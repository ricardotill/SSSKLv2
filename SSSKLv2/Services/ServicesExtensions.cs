using SSSKLv2.Services.Interfaces;
using SSSKLv2.Events;
using SSSKLv2.Services.Handlers;

namespace SSSKLv2.Services;

public static class ServicesExtensions
{
    public static IServiceCollection AddServicesDI(this IServiceCollection services)
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
            .AddTransient<IWebPushService, WebPushService>()
            .AddSingleton<IPurchaseNotifier, PurchaseNotifier>()
            .AddSingleton<IEventNotifier, EventNotifier>()
            .AddScoped<IDomainEventDispatcher, DomainEventDispatcher>()
            .AddScoped<IDomainEventHandler<OrderPlacedEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<TopUpEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<QuoteCreatedEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<QuoteVotedEvent>, AchievementEventHandler>()
            .AddScoped<IDomainEventHandler<ReactionAddedEvent>, AchievementEventHandler>()
            .AddHttpClient();
    }
}