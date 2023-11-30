using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data;

public class TopUp : BaseModel
{
    [Required]
    [DisplayName("Gebruiker")]
    public ApplicationUser User { get; set; }
    [Required]
    [DisplayName("Saldo")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Saldo { get; set; }
}