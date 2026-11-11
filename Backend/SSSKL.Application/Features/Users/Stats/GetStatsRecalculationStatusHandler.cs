using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Users.Stats;

internal sealed class GetStatsRecalculationStatusHandler(StatsRecalculationJob job)
    : IRequestHandler<GetStatsRecalculationStatusQuery, RecalculationJobDto?>
{
    public Task<RecalculationJobDto?> Handle(GetStatsRecalculationStatusQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(request.JobId is { } jobId ? job.GetStatus(jobId) : job.GetLatest());
}
