using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Users.Stats;

internal sealed class RecalculateUserStatsHandler(
    IApplicationUserRepository applicationUserRepository,
    IUserStatRepository userStatRepository) : IRequestHandler<RecalculateUserStatsCommand, RecalculateUserStatsResult>
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromDays(7);

    public async Task<RecalculateUserStatsResult> Handle(RecalculateUserStatsCommand request, CancellationToken cancellationToken)
    {
        await StatsAccess.EnsureCanAccess(applicationUserRepository, request.UserId, request.RequesterId, request.IsAdmin);

        if (!request.IsAdmin)
        {
            var claimed = await userStatRepository.TryMarkRecalculatedAtIfEligible(request.UserId, DateTime.UtcNow, MinInterval);
            if (!claimed)
            {
                var existingStats = await userStatRepository.GetOrCreateByUserId(request.UserId);
                return new RecalculateUserStatsResult(null, existingStats.LastStatsRecalculatedAt?.Add(MinInterval));
            }
        }

        var stats = await userStatRepository.RecalculateByUserId(request.UserId);
        return new RecalculateUserStatsResult(stats.ToDto(), null);
    }
}
