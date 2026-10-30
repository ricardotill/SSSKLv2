using SSSKL.Domain.Events;

namespace SSSKL.Contracts.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
}
