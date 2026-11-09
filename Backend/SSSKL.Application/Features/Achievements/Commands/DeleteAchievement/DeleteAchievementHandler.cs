using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Achievements.Commands.DeleteAchievement;

internal sealed class DeleteAchievementHandler(IAchievementRepository achievementRepository)
    : IRequestHandler<DeleteAchievementCommand>
{
    public Task Handle(DeleteAchievementCommand request, CancellationToken cancellationToken) =>
        achievementRepository.Delete(request.Id);
}
