using System;

namespace SSSKLv2.Dto.Api.v1;

public class AchievementEntryDto
{
    public Guid Id { get; set; }
    public Guid AchievementId { get; set; }
    public string AchievementName { get; set; } = string.Empty;
    public string AchievementDescription { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
    public string? ImageUrl { get; set; }
    public bool HasSeen { get; set; }
    public string? UserId { get; set; }
}

