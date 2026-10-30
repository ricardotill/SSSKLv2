using MediatR;

namespace SSSKL.Application.Features.Quotes.Commands.ToggleQuoteVote;

/// <summary>Adds or removes the user's vote. Returns true when the user now has a vote on the quote.</summary>
public sealed record ToggleQuoteVoteCommand(Guid QuoteId, string UserId) : IRequest<bool>;
