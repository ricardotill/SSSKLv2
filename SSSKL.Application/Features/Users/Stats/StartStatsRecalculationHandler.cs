using MediatR;

namespace SSSKL.Application.Features.Users.Stats;

internal sealed class StartStatsRecalculationHandler(StatsRecalculationJob job)
    : IRequestHandler<StartStatsRecalculationCommand, StartStatsRecalculationResult>
{
    public Task<StartStatsRecalculationResult> Handle(StartStatsRecalculationCommand request, CancellationToken cancellationToken)
    {
        var (status, started) = job.StartRecalculateAll(request.StartedByUserId);
        return Task.FromResult(new StartStatsRecalculationResult(status, started));
    }
}
