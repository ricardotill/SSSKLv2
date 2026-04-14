namespace SSSKLv2.Dto;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? LinkUri { get; set; }
    public DateTime CreatedOn { get; set; }
}
