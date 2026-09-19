using System.Collections.Concurrent;
using System.Text;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

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

        try
        {
            using var scope = scopeFactory.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var csv = await orderService.ExportAllOrdersToCsvAsync();

            job.CsvBytes = Encoding.UTF8.GetBytes(csv);
            job.Status = CsvExportJobStatus.Completed;
        }
        catch (Exception ex)
        {
            job.Status = CsvExportJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            logger.LogError(ex, "Order CSV export job {JobId} failed", job.Id);
        }
        finally
        {
            job.CompletedAt = DateTime.UtcNow;
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
