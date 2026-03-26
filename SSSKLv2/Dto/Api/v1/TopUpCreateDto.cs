using System.ComponentModel.DataAnnotations;

namespace SSSKLv2.Dto.Api.v1;

public class TopUpCreateDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Saldo must be greater than zero")]
    public decimal Saldo { get; set; }
}

