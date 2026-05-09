using SSSKLv2.Data;

namespace SSSKLv2.Events;

public record OrderPlacedEvent(Order Order) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
