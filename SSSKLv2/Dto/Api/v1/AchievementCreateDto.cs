using SSSKLv2.Data;

namespace SSSKLv2.Dto.Api.v1;

// JSON request body for creating an achievement, image sent as base64 rather than
// multipart/form-data: iOS PWAs with an active service worker are known to strip
// the body from multipart POST requests.
public class AchievementCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool AutoAchieve { get; set; }
    public Achievement.ActionOption Action { get; set; }
    public Achievement.ComparisonOperatorOption ComparisonOperator { get; set; }
    public int ComparisonValue { get; set; }
    public Base64FileUploadDto? Image { get; set; }
}
