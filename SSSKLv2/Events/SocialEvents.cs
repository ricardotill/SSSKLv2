using SSSKLv2.Data;

namespace SSSKLv2.Events;

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
