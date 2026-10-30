using System.Text.Json.Serialization;
using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Dto.Api.v1;

// JSON request body for creating an achievement, image sent as base64 rather than
// multipart/form-data: iOS PWAs with an active service worker are known to strip
// the body from multipart POST requests.
public class AchievementCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [JsonRequired]
    public bool AutoAchieve { get; set; }
    [JsonRequired]
    public Achievement.ActionOption Action { get; set; }
    [JsonRequired]
    public Achievement.ComparisonOperatorOption ComparisonOperator { get; set; }
    [JsonRequired]
    public int ComparisonValue { get; set; }
    public Base64FileUploadDto? Image { get; set; }
}
