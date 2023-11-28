using System.ComponentModel;

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
        public decimal Price { get; set; }
        [DisplayName("Voorraad")]
        public int Stock { get; set; }
    }
}
