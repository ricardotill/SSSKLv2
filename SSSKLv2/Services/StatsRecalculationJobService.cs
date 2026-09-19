using System.Collections.Concurrent;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

/// <summary>
/// Tracks bulk user-stats recalculation jobs in memory and runs them on a background thread.
/// Only one job may be active at a time.
/// </summary>
public class StatsRecalculationJobService(
    IServiceScopeFactory scopeFactory,
    ILogger<StatsRecalculationJobService> logger) : IStatsRecalculationJobService
{
    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();
    private readonly Lock _startLock = new();
    private Guid? _activeJobId;

    public (RecalculationJobDto Job, bool Started) StartRecalculateAll(string startedByUserId)
    {
        lock (_startLock)
        {
            if (_activeJobId is { } activeId &&
                _jobs.TryGetValue(activeId, out var activeJob) &&
                activeJob.Status is RecalculationJobStatus.Pending or RecalculationJobStatus.Running)
            {
                return (activeJob.ToDto(), false);
            }

            var job = new JobState
            {
                Id = Guid.NewGuid(),
                Status = RecalculationJobStatus.Pending,
                StartedAt = DateTime.UtcNow,
                StartedByUserId = startedByUserId
            };
            _jobs[job.Id] = job;
            _activeJobId = job.Id;

            _ = Task.Run(() => RunJobAsync(job));

            return (job.ToDto(), true);
        }
    }

    public RecalculationJobDto? GetStatus(Guid jobId)
        => _jobs.TryGetValue(jobId, out var job) ? job.ToDto() : null;

    public RecalculationJobDto? GetLatest()
        => _jobs.Values.OrderByDescending(j => j.StartedAt).FirstOrDefault()?.ToDto();

    private async Task RunJobAsync(JobState job)
    {
        job.Status = RecalculationJobStatus.Running;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
            var userStatRepository = scope.ServiceProvider.GetRequiredService<IUserStatRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var users = await userRepository.GetAll();
            var userIds = users.Select(user => user.Id).ToList();
            job.TotalUsers = userIds.Count;

            foreach (var userId in userIds)
            {
                try
                {
                    await userStatRepository.RecalculateByUserId(userId);
                }
                catch (Exception ex)
                {
                    job.FailedUsers++;
                    logger.LogError(ex,
                        "Failed to recalculate stats for user {UserId} during bulk recalculation job {JobId}",
                        userId, job.Id);
                }
                finally
                {
                    job.ProcessedUsers++;
                }
            }

            job.Status = RecalculationJobStatus.Completed;

            await notificationService.CreateNotificationAsync(
                job.StartedByUserId,
                "Herberekening voltooid",
                $"De herberekening van alle gebruikersstatistieken is voltooid ({job.ProcessedUsers - job.FailedUsers} van {job.TotalUsers} geslaagd, {job.FailedUsers} mislukt).",
                sendPush: true);
        }
        catch (Exception ex)
        {
            job.Status = RecalculationJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            logger.LogError(ex, "Bulk stats recalculation job {JobId} failed", job.Id);

            using var scope = scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notificationService.CreateNotificationAsync(
                job.StartedByUserId,
                "Herberekening mislukt",
                $"De herberekening van alle gebruikersstatistieken is mislukt: {ex.Message}",
                sendPush: true);
        }
        finally
        {
            job.CompletedAt = DateTime.UtcNow;
        }
    }

    private sealed class JobState
    {
        public required Guid Id { get; init; }
        public volatile RecalculationJobStatus Status;
        public int TotalUsers;
        public int ProcessedUsers;
        public int FailedUsers;
        public required DateTime StartedAt { get; init; }
        public DateTime? CompletedAt;
        public string? ErrorMessage;
        public required string StartedByUserId { get; init; }

        public RecalculationJobDto ToDto() => new()
        {
            Id = Id,
            Status = Status,
            TotalUsers = TotalUsers,
            ProcessedUsers = ProcessedUsers,
            FailedUsers = FailedUsers,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            ErrorMessage = ErrorMessage,
            StartedByUserId = StartedByUserId
        };
    }
}
