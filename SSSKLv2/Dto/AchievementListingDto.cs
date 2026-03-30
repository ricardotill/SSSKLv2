namespace SSSKLv2.Dto;

public record AchievementListingDto(
    Guid Id,
    string Name,
    string Description,
    DateTime? DateAdded,
    string? ImageUrl,
    bool Completed
);