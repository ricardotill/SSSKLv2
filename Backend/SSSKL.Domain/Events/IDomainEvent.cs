using MediatR;

namespace SSSKL.Domain.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
