using MediatR;
using SSSKL.Contracts.Events;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Events;

/// <summary>
/// Publishes domain events through MediatR in a new scope, so the handlers get their own unit of work
/// and don't share tracked entities with the request that raised the event.
/// </summary>
public class DomainEventDispatcher(IServiceScopeFactory scopeFactory) : IDomainEventDispatcher
{
    public async Task DispatchAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(domainEvent);
    }
}
