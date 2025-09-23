namespace SSSKLv2.Dto;

public record AchievementDto(
    string Name,
    string Description,
    string? ImageUrl,
    bool Completed
);