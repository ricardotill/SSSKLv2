using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Build.Framework;

namespace SSSKLv2.Data;

public class Order : BaseModel
{
    [Required]
    [DisplayName("Gebruiker")]
    public ApplicationUser User { get; set; }
    [Required]
    [DisplayName("Product")]
    public string ProductNaam { get; set; }
    [Required]
    [DisplayName("Hoeveelheid")]
    public int Amount { get; set; }
    [Required]
    [DisplayName("Betaald")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Paid { get; set; }
}