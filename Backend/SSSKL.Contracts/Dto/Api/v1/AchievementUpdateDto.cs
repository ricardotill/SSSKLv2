// filepath: /Users/ricardotill/Development/Repositories/SSSKLv2/SSSKLv2/Dto/AchievementUpdateDto.cs
using System.Text.Json.Serialization;
using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Dto.Api.v1;

public class AchievementImageDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class AchievementUpdateDto
{
    [JsonRequired]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [JsonRequired]
    public bool AutoAchieve { get; set; }
    [JsonRequired]
    public Achievement.ActionOption Action { get; set; } = Achievement.ActionOption.None;
    [JsonRequired]
    public Achievement.ComparisonOperatorOption ComparisonOperator { get; set; } = Achievement.ComparisonOperatorOption.None;
    [JsonRequired]
    public int ComparisonValue { get; set; }
    public string? Tier { get; set; }
    public Guid? ParentAchievementId { get; set; }
    public AchievementImageDto? Image { get; set; }
    // Base64 replacement image; set only when the admin picks a new file.
    public Base64FileUploadDto? NewImage { get; set; }
}

