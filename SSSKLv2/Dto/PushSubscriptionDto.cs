using System.ComponentModel.DataAnnotations;

namespace SSSKLv2.Dto;

public class PushSubscriptionDto
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string P256dh { get; set; } = string.Empty;

    [Required]
    public string Auth { get; set; } = string.Empty;
}
