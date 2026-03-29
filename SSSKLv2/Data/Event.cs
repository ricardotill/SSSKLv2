using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SSSKLv2.Data;

public class Event : BaseModel
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty; // Rich text

    public EventImage? Image { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [Required]
    public string CreatorId { get; set; } = string.Empty;

    [ForeignKey("CreatorId")]
    public ApplicationUser Creator { get; set; } = default!;

    public ICollection<EventResponse> Responses { get; set; } = new List<EventResponse>();
    
    public ICollection<IdentityRole> RequiredRoles { get; set; } = new List<IdentityRole>();
}
