using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Dto;

public class AchievementDto
{
    [Required]
    public required string Name { get; set; }
    public required string Description { get; set; }
    public Stream? ImageContent { get; set; }
    public ContentType? ImageContentType { get; set; }
    public bool AutoAchieve { get; set; }
    public Achievement.ActionOption Action { get; set; }
    public Achievement.ComparisonOperatorOption ComparisonOperator { get; set; }
    public int ComparisonValue { get; set; }
    public string? Tier { get; set; }
    public Guid? ParentAchievementId { get; set; }
}