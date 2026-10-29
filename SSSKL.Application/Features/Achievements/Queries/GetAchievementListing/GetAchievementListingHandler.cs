using MediatR;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementListing;

internal sealed class GetAchievementListingHandler(
    IAchievementRepository achievementRepository,
    IApplicationUserRepository applicationUserRepository)
    : IRequestHandler<GetAchievementListingQuery, IList<AchievementListingDto>>
{
    public async Task<IList<AchievementListingDto>> Handle(GetAchievementListingQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        if (userId == null)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return new List<AchievementListingDto>();

            var user = await applicationUserRepository.GetByUsername(request.Username);
            if (user == null)
                return new List<AchievementListingDto>();

            userId = user.Id;
        }

        var allAchievements = (await achievementRepository.GetAll()).ToList();
        var entries = await achievementRepository.GetAllEntriesOfUser(userId);
        return AchievementMappings.ToListing(allAchievements, entries);
    }
}
