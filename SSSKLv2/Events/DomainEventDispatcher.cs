using Microsoft.Extensions.DependencyInjection;

namespace SSSKLv2.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
}

public class DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            // Events are dispatched after the triggering change is committed. A failing handler
            // (achievements, stats) must not turn an already-saved order or top-up into an error response.
            try
            {
                await handler.HandleAsync(domainEvent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Domain event handler {Handler} failed for {Event}",
                    handler.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}
