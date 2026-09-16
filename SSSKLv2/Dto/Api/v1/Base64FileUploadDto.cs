namespace SSSKLv2.Dto.Api.v1;

// JSON-based file upload payload, used instead of multipart/form-data to avoid
// iOS PWA service worker stripping the body of multipart POST requests.
public class Base64FileUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Base64Content { get; set; } = string.Empty;
}
