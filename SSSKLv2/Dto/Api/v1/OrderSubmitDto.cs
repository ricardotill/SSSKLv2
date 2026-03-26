using System.ComponentModel.DataAnnotations;

namespace SSSKLv2.Dto.Api.v1;

public class OrderSubmitDto
{
    [MinLength(1, ErrorMessage = "Er moeten minimaal 1 of meer producten worden geselecteerd")]
    public IList<Guid> Products { get; set; } = new List<Guid>();
    [MinLength(1, ErrorMessage = "Er moeten minimaal 1 of meer gebruikers worden geselecteerd")]
    public IList<Guid> Users { get; set; } = new List<Guid>();
    [Required]
    public int Amount { get; set; } = 1;
    public bool Split { get; set; } = false;
}