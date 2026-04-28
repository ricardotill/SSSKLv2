using Microsoft.AspNetCore.Identity;

namespace SSSKLv2.Data
{
    public class Quote : BaseModel
    {
        public string Text { get; set; } = string.Empty;
        public DateTime DateSaid { get; set; } = DateTime.UtcNow;
        public string CreatedById { get; set; } = string.Empty;
        public ApplicationUser? CreatedBy { get; set; }
        
        public ICollection<QuoteAuthor> Authors { get; set; } = new List<QuoteAuthor>();
        public ICollection<IdentityRole> VisibleToRoles { get; set; } = new List<IdentityRole>();
    }
}
