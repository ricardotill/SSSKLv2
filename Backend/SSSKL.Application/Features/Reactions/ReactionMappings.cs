using SSSKL.Contracts.Dto;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Reactions;

public static class ReactionMappings
{
    public static ReactionDto ToDto(this Reaction r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.User?.UserName ?? "Unknown",
        ProfilePictureUrl = r.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{r.User.ProfileImageId}" : null,
        Content = r.Content,
        TargetId = r.TargetId,
        TargetType = r.TargetType.ToString(),
        CreatedOn = r.CreatedOn
    };

    public static ReactionTargetType ParseTargetType(string targetType) =>
        Enum.TryParse<ReactionTargetType>(targetType, true, out var parsed)
            ? parsed
            : throw new ArgumentException("Invalid target type");
}
