using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data;

public class Order : BaseModel
{
    [Microsoft.Build.Framework.Required]
    [DisplayName("Gebruiker")]
    public ApplicationUser User { get; set; }
    [Microsoft.Build.Framework.Required]
    [DisplayName("ProductNaam")]
    public string ProductName { get; set; }
    [DisplayName("Product")]
    public Product? Product { get; set; }
    [Microsoft.Build.Framework.Required]
    [DisplayName("Hoeveelheid")]
    public int Amount { get; set; }
    [Microsoft.Build.Framework.Required]
    [DisplayName("Betaald")]
    [Column(TypeName = "decimal(18,2)")]
    [RegularExpression(@"^\d+.\d{0,2}$",ErrorMessage = "Betaald moet 2 cijfers achter de komma hebben")]
    public decimal Paid { get; set; }
}