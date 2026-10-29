using Microsoft.Extensions.DependencyInjection;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Telemetry;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Events;

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
                SssklTelemetry.RecordSideEffectFailure(handler.GetType().Name);
                logger.LogError(ex, "Domain event handler {Handler} failed for {Event}",
                    handler.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}
