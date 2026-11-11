using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKL.Domain.Entities;

public class TopUp : BaseModel
{
    [Required]
    [DisplayName("Gebruiker")]
    public ApplicationUser User { get; set; } = null!;
    [Required]
    [DisplayName("Saldo")]
    [RegularExpression(@"^\d+.\d{0,2}$",ErrorMessage = "Saldo moet 2 cijfers achter de komma hebben")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Saldo { get; set; }
}