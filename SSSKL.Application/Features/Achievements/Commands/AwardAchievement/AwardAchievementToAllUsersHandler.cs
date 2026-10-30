using MediatR;
using SSSKL.Application.Shared;

namespace SSSKL.Application.Features.Achievements.Commands.AwardAchievement;

internal sealed class AwardAchievementToAllUsersHandler(IAchievementAwarder awarder)
    : IRequestHandler<AwardAchievementToAllUsersCommand, int>
{
    public Task<int> Handle(AwardAchievementToAllUsersCommand request, CancellationToken cancellationToken) =>
        awarder.AwardToAllUsers(request.AchievementId);
}
