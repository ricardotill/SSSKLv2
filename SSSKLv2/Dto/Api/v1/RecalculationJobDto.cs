namespace SSSKLv2.Dto.Api.v1;

public enum RecalculationJobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public class RecalculationJobDto
{
    public Guid Id { get; set; }
    public RecalculationJobStatus Status { get; set; }
    public int TotalUsers { get; set; }
    public int ProcessedUsers { get; set; }
    public int FailedUsers { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string StartedByUserId { get; set; } = string.Empty;
}
