using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SSSKLv2.Data
{
    public class Product : BaseModel
    {
        [Required]
        [DisplayName("Naam")]
        public string Name { get; set; } = string.Empty;
        
        [DisplayName("Beschrijving")]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [DisplayName("Prijs")]
        [Column(TypeName = "decimal(18,2)")]
        [RegularExpression(@"^\d+.\d{0,2}$",ErrorMessage = "Prijs moet 2 cijfers achter de komma hebben")]
        public decimal Price { get; set; }
        
        [Required]
        [DisplayName("Voorraad")]
        public int Stock { get; set; }
        
        [DisplayName("Bestellingen")] 
        public IList<Order> Orders { get; set; } = new List<Order>();
    }
}
