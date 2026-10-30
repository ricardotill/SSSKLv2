using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementEntries;

internal sealed class GetAchievementEntriesHandler(
    IAchievementRepository achievementRepository,
    IApplicationUserRepository applicationUserRepository)
    : IRequestHandler<GetAchievementEntriesQuery, IReadOnlyList<AchievementEntryDto>>
{
    public async Task<IReadOnlyList<AchievementEntryDto>> Handle(GetAchievementEntriesQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        if (userId == null)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return [];

            var user = await applicationUserRepository.GetByUsername(request.Username);
            if (user == null)
                return [];

            userId = user.Id;
        }

        var entries = await achievementRepository.GetAllEntriesOfUser(userId);
        return entries.Select(e => e.ToDto()).ToList();
    }
}
