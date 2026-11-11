using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Achievements;

public static class AchievementMappings
{
    public static AchievementResponseDto ToResponseDto(this Achievement a) => new()
    {
        Id = a.Id,
        Name = a.Name ?? string.Empty,
        Description = a.Description ?? string.Empty,
        AutoAchieve = a.AutoAchieve,
        Action = a.Action,
        ComparisonOperator = a.ComparisonOperator,
        ComparisonValue = a.ComparisonValue,
        Tier = a.Tier.ToString(),
        ParentAchievementId = a.ParentAchievementId,
        Image = a.Image == null ? null : new AchievementImageDto
        {
            Id = a.Image.Id,
            FileName = a.Image.FileName,
            Uri = $"/api/v1/blob/achievement/image/{a.Image.Id}",
            ContentType = a.Image.ContentType
        }
    };

    public static AchievementEntryDto ToDto(this AchievementEntry e) => new()
    {
        Id = e.Id,
        AchievementId = e.Achievement?.Id ?? Guid.Empty,
        AchievementName = e.Achievement?.Name ?? string.Empty,
        AchievementDescription = e.Achievement?.Description ?? string.Empty,
        DateAdded = e.CreatedOn,
        ImageUrl = e.Achievement?.Image != null ? $"/api/v1/blob/achievement/image/{e.Achievement.Image.Id}" : null,
        HasSeen = e.HasSeen,
        Tier = e.Tier.ToString(),
        ParentAchievementId = e.Achievement?.ParentAchievementId,
        ParentAchievementName = e.Achievement?.ParentAchievement?.Name,
        UserId = e.User?.Id,
        UserName = e.User?.UserName,
        UserFullName = e.User?.FullName,
        UserProfilePictureUrl = e.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{e.User.ProfileImageId}" : null
    };

    /// <summary>Lists every achievement, flagged as completed when the user has an entry for it.</summary>
    public static IList<AchievementListingDto> ToListing(IList<Achievement> allAchievements, IList<AchievementEntry> userEntries) =>
        allAchievements.Select(a =>
        {
            var entry = userEntries.SingleOrDefault(e => e.Achievement.Id == a.Id);
            var parent = a.ParentAchievementId.HasValue
                ? allAchievements.FirstOrDefault(x => x.Id == a.ParentAchievementId.Value)
                : null;
            return new AchievementListingDto(
                a.Id,
                a.Name,
                a.Description,
                entry?.CreatedOn,
                a.Image != null ? $"/api/v1/blob/achievement/image/{a.Image.Id}" : null,
                entry != null,
                a.Tier.ToString(),
                parent?.Id,
                parent?.Name
            );
        }).ToList();
}
