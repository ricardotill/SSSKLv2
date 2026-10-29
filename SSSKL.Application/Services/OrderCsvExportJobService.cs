using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Services;
using SSSKL.Contracts.Telemetry;

namespace SSSKL.Application.Services;

/// <summary>
/// Tracks admin order CSV export jobs in memory and generates exports in the background.
/// Only one export may be active at a time.
/// </summary>
public class OrderCsvExportJobService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderCsvExportJobService> logger) : IOrderCsvExportJobService
{
    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();
    private readonly Lock _startLock = new();
    private Guid? _activeJobId;

    public (CsvExportJobDto Job, bool Started) StartExport(string startedByUserId)
    {
        lock (_startLock)
        {
            if (_activeJobId is { } activeId &&
                _jobs.TryGetValue(activeId, out var activeJob) &&
                activeJob.Status is CsvExportJobStatus.Pending or CsvExportJobStatus.Running)
            {
                return (activeJob.ToDto(), false);
            }

            var job = new JobState
            {
                Id = Guid.NewGuid(),
                Status = CsvExportJobStatus.Pending,
                StartedAt = DateTime.UtcNow,
                StartedByUserId = startedByUserId,
                FileName = $"Orders_Export_{DateTime.UtcNow:yyyy-MM-dd}.csv"
            };

            _jobs[job.Id] = job;
            _activeJobId = job.Id;

            _ = Task.Run(() => RunJobAsync(job));

            return (job.ToDto(), true);
        }
    }

    public CsvExportJobDto? GetStatus(Guid jobId)
        => _jobs.TryGetValue(jobId, out var job) ? job.ToDto() : null;

    public (byte[] Bytes, string FileName)? GetCsv(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job) ||
            job.Status != CsvExportJobStatus.Completed ||
            job.CsvBytes == null)
        {
            return null;
        }

        return (job.CsvBytes, job.FileName);
    }

    private async Task RunJobAsync(JobState job)
    {
        job.Status = CsvExportJobStatus.Running;
        using var activity = SssklTelemetry.ActivitySource.StartActivity("OrderCsvExportJob");
        activity?.SetTag("job.id", job.Id);
        logger.LogInformation("Order CSV export job {JobId} started", job.Id);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var csv = await orderService.ExportAllOrdersToCsvAsync();

            job.CsvBytes = Encoding.UTF8.GetBytes(csv);
            // Stamp CompletedAt before publishing the terminal status: pollers read both without a lock
            job.CompletedAt = DateTime.UtcNow;
            job.Status = CsvExportJobStatus.Completed;
        }
        catch (Exception ex)
        {
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.Status = CsvExportJobStatus.Failed;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Order CSV export job {JobId} failed", job.Id);
        }
        finally
        {
            job.CompletedAt ??= DateTime.UtcNow;
            var duration = job.CompletedAt.Value - job.StartedAt;
            SssklTelemetry.RecordJob("order_csv_export", job.Status.ToString().ToLowerInvariant(), duration);
            logger.LogInformation("Order CSV export job {JobId} finished as {Status} in {DurationMs} ms",
                job.Id, job.Status, (long)duration.TotalMilliseconds);
        }
    }

    private sealed class JobState
    {
        public required Guid Id { get; init; }
        public volatile CsvExportJobStatus Status;
        public required DateTime StartedAt { get; init; }
        public DateTime? CompletedAt;
        public string? ErrorMessage;
        public required string StartedByUserId { get; init; }
        public required string FileName { get; init; }
        public byte[]? CsvBytes;

        public CsvExportJobDto ToDto() => new()
        {
            Id = Id,
            Status = Status,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            ErrorMessage = ErrorMessage,
            StartedByUserId = StartedByUserId,
            FileName = FileName
        };
    }
}
