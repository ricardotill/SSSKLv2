// filepath: /Users/ricardotill/Development/Repositories/SSSKLv2/SSSKLv2/Dto/AchievementUpdateDto.cs
using SSSKLv2.Data;

namespace SSSKLv2.Dto.Api.v1;

public class AchievementImageDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class AchievementUpdateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool AutoAchieve { get; set; }
    public Achievement.ActionOption Action { get; set; } = Achievement.ActionOption.None;
    public Achievement.ComparisonOperatorOption ComparisonOperator { get; set; } = Achievement.ComparisonOperatorOption.None;
    public int ComparisonValue { get; set; }
    public AchievementImageDto? Image { get; set; }
}

