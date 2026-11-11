using SSSKL.Domain.Entities;


namespace SSSKL.Domain.Events;

public record TopUpEvent(TopUp TopUp) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
