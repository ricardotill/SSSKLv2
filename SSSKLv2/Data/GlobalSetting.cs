using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data;

public class GlobalSetting : BaseModel
{
    [Required]
    [MaxLength(150)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}
