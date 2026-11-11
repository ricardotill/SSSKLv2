using MediatR;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Reactions.Queries.GetReactionTimeline;

internal sealed class GetReactionTimelineHandler(IReactionRepository reactionRepository)
    : IRequestHandler<GetReactionTimelineQuery, IReadOnlyList<ReactionDto>>
{
    public async Task<IReadOnlyList<ReactionDto>> Handle(GetReactionTimelineQuery request, CancellationToken cancellationToken)
    {
        var reactions = await reactionRepository.GetTimeline(request.Skip, request.Take);
        return reactions.Select(r => r.ToDto()).ToList();
    }
}
