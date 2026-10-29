using System.Text.Json.Serialization;

namespace SSSKLv2.Dto.Api.v1;

public class AnnouncementCreateDto
{
    public string Message { get; set; } = string.Empty;
    public string? Description { get; set; }
    [JsonRequired]
    public int Order { get; set; }
    [JsonRequired]
    public bool IsScheduled { get; set; }
    public DateTime? PlannedFrom { get; set; }
    public DateTime? PlannedTill { get; set; }
}
