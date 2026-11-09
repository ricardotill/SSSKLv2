using MediatR;
using SSSKL.Application.Shared;

namespace SSSKL.Application.Features.Achievements.Commands.AwardAchievement;

internal sealed class AwardAchievementToUserHandler(IAchievementAwarder awarder)
    : IRequestHandler<AwardAchievementToUserCommand, bool>
{
    public Task<bool> Handle(AwardAchievementToUserCommand request, CancellationToken cancellationToken) =>
        awarder.AwardToUser(request.UserId, request.AchievementId);
}
