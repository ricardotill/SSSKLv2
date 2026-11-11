using SSSKL.Domain.Entities;


namespace SSSKL.Domain.Events;

public record OrderPlacedEvent(Order Order) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
