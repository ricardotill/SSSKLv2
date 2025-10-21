using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using SSSKLv2.Data;

namespace SSSKLv2.Dto;

public class AchievementDto
{
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    public Stream ImageContent { get; set; }
    public ContentType ImageContentType { get; set; }
    public bool AutoAchieve { get; set; }
    public Achievement.ActionOption Action { get; set; }
    public Achievement.ComparisonOperatorOption ComparisonOperator { get; set; }
    public int ComparisonValue { get; set; }
}