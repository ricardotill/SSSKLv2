using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievements;

internal sealed class GetAchievementsHandler(IAchievementRepository achievementRepository)
    : IRequestHandler<GetAchievementsQuery, PaginationObject<AchievementResponseDto>>
{
    public async Task<PaginationObject<AchievementResponseDto>> Handle(GetAchievementsQuery request, CancellationToken cancellationToken)
    {
        var achievements = await achievementRepository.GetAll(request.Skip, request.Take);
        var totalCount = await achievementRepository.GetCount();

        return new PaginationObject<AchievementResponseDto>
        {
            Items = achievements.Select(a => a.ToResponseDto()).ToList(),
            TotalCount = totalCount
        };
    }
}
