namespace SSSKLv2.Dto;

public class LeaderboardEntryDto
{
    public int Position { get; set; }
    public required string FullName { get; set; }
    public required string ProductName { get; set; }
    public int Amount { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? UserId { get; set; }
}