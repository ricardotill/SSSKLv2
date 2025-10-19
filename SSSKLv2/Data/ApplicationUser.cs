using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

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
        public IEnumerable<AchievementEntry> CompletedAchievements { get; set; } = new List<AchievementEntry>();
        [Microsoft.Build.Framework.Required]
        public DateTime LastOrdered { get; set; } = DateTime.Now;
        public byte[]? ProfilePicture { get; set; }
        
        public string FullName => $"{Name} {Surname.First()}";
    }

}
