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
        public decimal Saldo { get; set; }
    }

}
