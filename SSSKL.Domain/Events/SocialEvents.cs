using SSSKL.Domain.Entities;


namespace SSSKL.Domain.Events;

public record QuoteCreatedEvent(Quote Quote) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record QuoteVotedEvent(QuoteVote Vote) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ReactionAddedEvent(Reaction Reaction) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
