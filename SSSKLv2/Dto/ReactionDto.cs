using System.Collections.Generic;

namespace SSSKLv2.Dto;

public class ReactionDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string? TargetUserName { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<ReactionDto> Reactions { get; set; } = new();
}

public class ToggleReactionRequest
{
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
