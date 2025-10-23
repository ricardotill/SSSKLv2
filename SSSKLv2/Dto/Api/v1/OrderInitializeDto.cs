using System.ComponentModel.DataAnnotations;

namespace SSSKLv2.Dto.Api.v1;

public class OrderInitializeProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Stock { get; set; }
}

public class OrderInitializeUserDto
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class OrderInitializeDto
{
    [MinLength(1, ErrorMessage = "Er moeten minimaal 1 of meer producten worden geselecteerd")]
    public IList<OrderInitializeProductDto> Products { get; set; } = new List<OrderInitializeProductDto>();
    [MinLength(1, ErrorMessage = "Er moeten minimaal 1 of meer gebruikers worden geselecteerd")]
    public IList<OrderInitializeUserDto> Users { get; set; } = new List<OrderInitializeUserDto>();
    [Required]
    public int Amount { get; set; } = 1;
    public bool Split { get; set; } = false;
}