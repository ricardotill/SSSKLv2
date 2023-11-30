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
        public string Name { get; set; }
        [DisplayName("Beschrijving")]
        public string Description { get; set; }
        [Required]
        [DisplayName("Prijs")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Required]
        [DisplayName("Voorraad")]
        public int Stock { get; set; }
    }
}
