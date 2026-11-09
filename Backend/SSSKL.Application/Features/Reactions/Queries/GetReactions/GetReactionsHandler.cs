using MediatR;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Reactions.Queries.GetReactions;

internal sealed class GetReactionsHandler(IReactionRepository reactionRepository)
    : IRequestHandler<GetReactionsQuery, IReadOnlyList<ReactionDto>>
{
    public async Task<IReadOnlyList<ReactionDto>> Handle(GetReactionsQuery request, CancellationToken cancellationToken)
    {
        var targetType = ReactionMappings.ParseTargetType(request.TargetType);

        var topLevelReactions = await reactionRepository.GetByTargets(targetType, [request.TargetId]);
        var topLevelDtos = topLevelReactions.Select(r => r.ToDto()).ToList();
        if (topLevelDtos.Count == 0)
            return topLevelDtos;

        // Level 2: Replies targeting Level 1
        var level2Dtos = await GetReplies(topLevelDtos);
        if (level2Dtos.Count == 0)
            return topLevelDtos;

        // Level 3: Replies targeting Level 2
        var level3Dtos = await GetReplies(level2Dtos);

        // For a "flat thread" feel, all level 2 and level 3 replies are shown under the level 1 root.
        foreach (var parent in topLevelDtos)
        {
            var directReplies = level2Dtos.Where(r => r.TargetId == parent.Id).ToList();
            var indirectReplies = level3Dtos.Where(r => directReplies.Any(dr => dr.Id == r.TargetId)).ToList();

            parent.Reactions = directReplies.Concat(indirectReplies).OrderBy(r => r.CreatedOn).ToList();
        }

        return topLevelDtos;
    }

    private async Task<List<ReactionDto>> GetReplies(List<ReactionDto> parents)
    {
        var replies = await reactionRepository.GetByTargets(ReactionTargetType.Reaction, parents.Select(r => r.Id).ToList());

        return replies.Select(r =>
        {
            var dto = r.ToDto();
            dto.TargetUserName = parents.FirstOrDefault(t => t.Id == r.TargetId)?.UserName;
            return dto;
        }).ToList();
    }
}
