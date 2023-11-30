using SSSKLv2.Data;

namespace SSSKLv2.DTO;

public class ApplicationUserPaged
{
    public IList<ApplicationUser> ApplicationUsers { get; set; }
    public int ApplicationUserCount { get; set; }
}