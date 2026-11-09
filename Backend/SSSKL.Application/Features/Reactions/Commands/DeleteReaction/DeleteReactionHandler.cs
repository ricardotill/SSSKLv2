using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Reactions.Commands.DeleteReaction;

internal sealed class DeleteReactionHandler(IReactionRepository reactionRepository, IUserStatRepository userStatRepository)
    : IRequestHandler<DeleteReactionCommand>
{
    public async Task Handle(DeleteReactionCommand request, CancellationToken cancellationToken)
    {
        var reaction = await reactionRepository.GetById(request.Id);
        if (reaction == null) return;

        if (!request.IsAdmin && reaction.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this reaction.");
        }

        var affectedUserIds = await reactionRepository.DeleteWithReplies(request.Id);
        foreach (var affectedUserId in affectedUserIds)
        {
            await userStatRepository.RecalculateByUserId(affectedUserId);
        }
    }
}
