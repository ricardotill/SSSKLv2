using SSSKL.Application.Shared;
using System.Diagnostics;
using System.Collections.Concurrent;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Contracts.Telemetry;

namespace SSSKL.Application.Services;

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
        using var activity = SssklTelemetry.ActivitySource.StartActivity("StatsRecalculationJob");
        activity?.SetTag("job.id", job.Id);
        logger.LogInformation("Bulk stats recalculation job {JobId} started", job.Id);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
            var userStatRepository = scope.ServiceProvider.GetRequiredService<IUserStatRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IUserNotifier>();

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

            // Stamp CompletedAt before publishing the terminal status: pollers read both without a lock
            job.CompletedAt = DateTime.UtcNow;
            job.Status = RecalculationJobStatus.Completed;
            activity?.SetTag("job.users.total", job.TotalUsers);
            activity?.SetTag("job.users.failed", job.FailedUsers);

            await notificationService.NotifyAsync(
                job.StartedByUserId,
                "Herberekening voltooid",
                $"De herberekening van alle gebruikersstatistieken is voltooid ({job.ProcessedUsers - job.FailedUsers} van {job.TotalUsers} geslaagd, {job.FailedUsers} mislukt).",
                sendPush: true);
        }
        catch (Exception ex)
        {
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.Status = RecalculationJobStatus.Failed;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Bulk stats recalculation job {JobId} failed", job.Id);

            using var scope = scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IUserNotifier>();
            await notificationService.NotifyAsync(
                job.StartedByUserId,
                "Herberekening mislukt",
                $"De herberekening van alle gebruikersstatistieken is mislukt: {ex.Message}",
                sendPush: true);
        }
        finally
        {
            job.CompletedAt ??= DateTime.UtcNow;
            var duration = job.CompletedAt.Value - job.StartedAt;
            SssklTelemetry.RecordJob("stats_recalculation", job.Status.ToString().ToLowerInvariant(), duration);
            logger.LogInformation("Bulk stats recalculation job {JobId} finished as {Status} in {DurationMs} ms ({FailedUsers}/{TotalUsers} users failed)",
                job.Id, job.Status, (long)duration.TotalMilliseconds, job.FailedUsers, job.TotalUsers);
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
