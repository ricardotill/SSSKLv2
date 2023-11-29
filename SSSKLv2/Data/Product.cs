using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data
{
    public class Product
    {
        public Guid Id { get; set; }
        [DisplayName("Naam")]
        public string Name { get; set; }
        [DisplayName("Beschrijving")]
        public string Description { get; set; }
        [DisplayName("Prijs")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [DisplayName("Voorraad")]
        public int Stock { get; set; }
    }
}
