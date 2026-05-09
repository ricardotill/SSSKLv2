namespace SSSKLv2.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
