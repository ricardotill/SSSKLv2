using SSSKLv2.Data;

namespace SSSKLv2.Events;

public record TopUpEvent(TopUp TopUp) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
