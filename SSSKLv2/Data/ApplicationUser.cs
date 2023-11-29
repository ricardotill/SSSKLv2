using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SSSKLv2.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string Name { get; set; }
        [PersonalData]
        public string Surname {  get; set; }
        [PersonalData]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; }
        public byte[]? ProfilePicture { get; set; }
    }

}
