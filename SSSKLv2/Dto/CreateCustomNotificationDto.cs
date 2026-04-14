using System.ComponentModel.DataAnnotations;

namespace SSSKLv2.Dto;

public class CreateCustomNotificationDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters.", MinimumLength = 1)]
    public required string Title { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    public required string Message { get; set; }

    [MaxLength(2048, ErrorMessage = "Link URI is too long.")]
    [RegularExpression(@"^/.*$", ErrorMessage = "Link URI must be a relative path starting with '/'. External URLs are not allowed.")]
    public string? LinkUri { get; set; }

    public bool FanOut { get; set; } = true;

    public List<string>? UserIds { get; set; }
}
