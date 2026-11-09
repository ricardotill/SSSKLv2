using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKL.Domain.Entities;

public class Order : BaseModel
{
    [DisplayName("Gebruiker")]
    public ApplicationUser User { get; set; } = null!;
    [DisplayName("ProductNaam")]
    public string ProductNaam { get; set; } = string.Empty;
    [DisplayName("Product")]
    public Product? Product { get; set; }
    [DisplayName("Hoeveelheid")]
    public int Amount { get; set; }
    [DisplayName("Betaald")]
    [Column(TypeName = "decimal(18,2)")]
    [RegularExpression(@"^\d+.\d{0,2}$",ErrorMessage = "Betaald moet 2 cijfers achter de komma hebben")]
    public decimal Paid { get; set; }
}