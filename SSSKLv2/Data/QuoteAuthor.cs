using Microsoft.AspNetCore.Identity;

namespace SSSKLv2.Data
{
    public class QuoteAuthor : BaseModel
    {
        public Guid QuoteId { get; set; }
        public Quote? Quote { get; set; }
        
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
        
        public string? CustomName { get; set; }
    }
}
