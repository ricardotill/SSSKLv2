using SSSKLv2.Data;

namespace SSSKLv2.Dto.Api.v1;

public class AchievementResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool AutoAchieve { get; set; }
    public Achievement.ActionOption Action { get; set; }
    public Achievement.ComparisonOperatorOption ComparisonOperator { get; set; }
    public int ComparisonValue { get; set; }
    public AchievementImageDto? Image { get; set; }
}

