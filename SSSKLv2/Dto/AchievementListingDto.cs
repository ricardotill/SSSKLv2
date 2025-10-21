namespace SSSKLv2.Dto;

public record AchievementListingDto(
    string Name,
    string Description,
    DateTime? DateAdded,
    string? ImageUrl,
    bool Completed
);