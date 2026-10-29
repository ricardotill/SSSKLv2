using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Achievements.Commands.DeleteAchievementEntries;

internal sealed class DeleteAchievementEntriesHandler(IAchievementRepository achievementRepository)
    : IRequestHandler<DeleteAchievementEntriesCommand>
{
    public Task Handle(DeleteAchievementEntriesCommand request, CancellationToken cancellationToken) =>
        achievementRepository.DeleteAchievementEntryRange(request.EntryIds.Select(id => new AchievementEntry { Id = id }));
}
