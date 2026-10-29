namespace SSSKL.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
