using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data;

public class Reaction : BaseModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = default!;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public Guid TargetId { get; set; }

    [Required]
    public ReactionTargetType TargetType { get; set; }
}
