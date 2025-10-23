namespace SSSKLv2.Dto.Api.v1;

public class AnnouncementUpdateDto
{
    public string Message { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FotoUrl { get; set; }
    public string? Url { get; set; }
    public int Order { get; set; }
    public bool IsScheduled { get; set; }
    public DateTime? PlannedFrom { get; set; }
    public DateTime? PlannedTill { get; set; }
}
