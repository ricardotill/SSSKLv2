using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Achievements.Commands.MarkUnseenEntriesSeen;

internal sealed class MarkUnseenEntriesSeenHandler(IAchievementRepository achievementRepository)
    : IRequestHandler<MarkUnseenEntriesSeenCommand, IReadOnlyList<AchievementEntryDto>>
{
    public async Task<IReadOnlyList<AchievementEntryDto>> Handle(MarkUnseenEntriesSeenCommand request, CancellationToken cancellationToken)
    {
        var entries = (await achievementRepository.GetPersonalUnseenAchievementEntries(request.Username))?.ToList() ?? [];

        if (entries.Count > 0)
        {
            foreach (var entry in entries)
            {
                entry.HasSeen = true;
            }

            await achievementRepository.UpdateAchievementEntryRange(entries);
        }

        return entries.Select(e => e.ToDto()).ToList();
    }
}
