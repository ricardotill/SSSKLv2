using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Services.Interfaces;

public interface IStatsRecalculationJobService
{
    /// <summary>Starts a new bulk recalculation job unless one is already pending/running, in which case the existing job is returned.</summary>
    (RecalculationJobDto Job, bool Started) StartRecalculateAll(string startedByUserId);
    RecalculationJobDto? GetStatus(Guid jobId);
    RecalculationJobDto? GetLatest();
}
