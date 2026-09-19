using System.Text.Json.Serialization;

namespace SSSKLv2.Dto.Api.v1;

[JsonConverter(typeof(JsonStringEnumConverter<CsvExportJobStatus>))]
public enum CsvExportJobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public class CsvExportJobDto
{
    public Guid Id { get; set; }
    public CsvExportJobStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string StartedByUserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
