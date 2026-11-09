using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementEarners;

internal sealed class GetAchievementEarnersHandler(IAchievementRepository achievementRepository)
    : IRequestHandler<GetAchievementEarnersQuery, IReadOnlyList<AchievementEntryDto>>
{
    public async Task<IReadOnlyList<AchievementEntryDto>> Handle(GetAchievementEarnersQuery request, CancellationToken cancellationToken)
    {
        var entries = await achievementRepository.GetAllEntries(request.AchievementId);
        return entries.Select(e => e.ToDto()).ToList();
    }
}
