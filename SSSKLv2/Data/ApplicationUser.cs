using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Framework;

namespace SSSKLv2.Data
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string Name { get; set; }
        [PersonalData]
        public string Surname {  get; set; }
        [PersonalData]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; }
        [PersonalData]
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();
        [PersonalData]
        public IEnumerable<TopUp> TopUps { get; set; } = new List<TopUp>();
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime LastOrdered { get; set; } = DateTime.UtcNow;
        public byte[]? ProfilePicture { get; set; }
    }

}
