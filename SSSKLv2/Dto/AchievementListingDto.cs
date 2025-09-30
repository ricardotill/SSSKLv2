namespace SSSKLv2.Dto;

public record AchievementListingDto(
    string Name,
    string Description,
    string? ImageUrl,
    bool Completed
);