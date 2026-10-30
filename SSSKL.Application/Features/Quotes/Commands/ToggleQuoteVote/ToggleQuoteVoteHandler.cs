using MediatR;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Features.Quotes.Commands.ToggleQuoteVote;

internal sealed class ToggleQuoteVoteHandler(
    IQuoteRepository quoteRepository,
    IUserStatRepository userStatRepository,
    IDomainEventDispatcher eventDispatcher,
    QuoteAccess quoteAccess) : IRequestHandler<ToggleQuoteVoteCommand, bool>
{
    public async Task<bool> Handle(ToggleQuoteVoteCommand request, CancellationToken cancellationToken)
    {
        await quoteAccess.EnsureUserHasAccess(request.UserId);
        var existingVote = await quoteRepository.GetVote(request.QuoteId, request.UserId);

        if (existingVote != null)
        {
            await quoteRepository.DeleteVote(existingVote);

            var affectedUserIds = await quoteRepository.GetAuthorUserIds(request.QuoteId);
            if (!affectedUserIds.Contains(request.UserId))
            {
                affectedUserIds.Add(request.UserId);
            }

            foreach (var affectedUserId in affectedUserIds)
            {
                await userStatRepository.RecalculateByUserId(affectedUserId);
            }
            return false;
        }

        var newVote = new QuoteVote
        {
            QuoteId = request.QuoteId,
            UserId = request.UserId
        };

        await quoteRepository.AddVote(newVote);
        await eventDispatcher.DispatchAsync(new QuoteVotedEvent(newVote));
        return true;
    }
}
