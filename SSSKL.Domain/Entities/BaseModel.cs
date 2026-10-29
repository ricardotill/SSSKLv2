using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKL.Domain.Entities;

public class BaseModel
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}